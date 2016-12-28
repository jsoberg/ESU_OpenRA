using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.AI.Esu.Strategy;
using OpenRA.Mods.Common.AI.Esu.Strategy.Scouting;

namespace OpenRA.Mods.Common.AI.Esu.Rules.Units
{
    class ScoutHelper
    {
        private readonly World world;
        private readonly Player selfPlayer;
        private readonly EsuAIInfo info;

        private readonly List<ScoutActor> currentScouts;
        private readonly List<ScoutActor> deadScouts;
        private readonly ScoutTargetLocationPool targetPool;

        private string scoutInProductionName;

        public ScoutHelper(World world, Player selfPlayer, EsuAIInfo info)
        {
            this.world = world;
            this.selfPlayer = selfPlayer;
            this.info = info;

            this.currentScouts = new List<ScoutActor>();
            this.deadScouts = new List<ScoutActor>();
            this.targetPool = new ScoutTargetLocationPool(selfPlayer);
        }

        // @return - returns true if the produced unit was claimed, false otherwise.
        public bool UnitProduced(Actor producer, Actor other)
        {
            if (other.Info.Name == scoutInProductionName) {
                AddActorAsScout(other);
                return true;
            }
            return false;
        }

        private void AddActorAsScout(Actor actor)
        {
            currentScouts.Add(new ScoutActor(actor));
            scoutInProductionName = null;
        }

        public void AddScoutOrdersIfApplicable(Actor self, StrategicWorldState state, Queue<Order> orders) 
        {
            IssueBuildScoutOrdersIfApplicable(self, state, orders);
            PerformCurrentScoutMaintenance(state, orders);
        }

        // ========================================
        // Scout Build Orders
        // ========================================

        private void IssueBuildScoutOrdersIfApplicable(Actor self, StrategicWorldState state, Queue<Order> orders)
        {
            if (!ShouldBuildNewScout(state)) {
                return;
            }

            AddBuildNewScoutOrder(self, state, orders);
        }

        private bool ShouldBuildNewScout(StrategicWorldState state)
        {
            if (scoutInProductionName != null || currentScouts.Count >= info.NumberOfScoutsToProduce) {
                return false;
            }

            var productionQueues = EsuAIUtils.FindProductionQueuesForPlayerAndCategory(world, selfPlayer, EsuAIConstants.ProductionCategories.INFANTRY);
            if (productionQueues.Count() == 0) {
                // We aren't able to build a scout right now.
                return false;
            }

            return true;
        }

        private void AddBuildNewScoutOrder(Actor self, StrategicWorldState state, Queue<Order> orders)
        {
            scoutInProductionName = GetBestAvailableScoutName();
            if (scoutInProductionName == null) {
                return;
            }

            orders.Enqueue(Order.StartProduction(self, scoutInProductionName, 1));
            // Try to consume this scout as an existing unit if one exists.
            TryObtainScoutNow(state);
        }

        [Desc("Uses the current world state to find the best available scouting unit to build.")]
        private string GetBestAvailableScoutName()
        {
            var productionQueues = EsuAIUtils.FindProductionQueuesForPlayerAndCategory(world, selfPlayer, EsuAIConstants.ProductionCategories.INFANTRY);
            foreach (ProductionQueue queue in productionQueues) {
                // TODO faction checks for dogs?
                if (queue.BuildableItems().Count(a => a.Name == EsuAIConstants.Infantry.RIFLE_INFANTRY) > 0) {
                    return EsuAIConstants.Infantry.RIFLE_INFANTRY;
                }
            }

            return null;
        }

        private void TryObtainScoutNow(StrategicWorldState state)
        {
            // All available actors (living actors owned by the player with the scout in production name which are not currently scouts and are not currently involved in an attack).
            var availableActors = state.World.Actors.Where(a => a.Owner == selfPlayer && !a.IsDead && a.Info.Name == scoutInProductionName 
                && !currentScouts.Any(sa => sa.Actor == a) && !state.ActiveAttackController.IsActorInvolvedInActiveAttack(a));

            // Grab first available unit as scout.
            if (availableActors.Count() > 0) {
                AddActorAsScout(availableActors.First());
            }
        }

        // ========================================
        // Scout Movement/ Upkeep
        // ========================================

        private void PerformCurrentScoutMaintenance(StrategicWorldState state, Queue<Order> orders)
        {
            RemoveDeadScouts();
            if (currentScouts.Count() == 0) {
                return;
            }

            IssueMovementOrdersForScouts(state, orders);
            IssueScoutReports(state);
        }

        private void RemoveDeadScouts()
        {
            for (int i = (currentScouts.Count() - 1); i >= 0; i--) {
                ScoutActor scout = currentScouts[i];
                if (scout.Actor.IsDead) {
                    currentScouts.RemoveAt(i);
                    deadScouts.Add(scout);
                }
            }
        }

        // ========================================
        // Scout Movement
        // ========================================

        private void IssueMovementOrdersForScouts(StrategicWorldState state, Queue<Order> orders)
        {
            foreach (ScoutActor scout in currentScouts) {
                scout.ProductionCooldown--;
                scout.MovementCooldown--;

                if (!scout.HasTarget() && scout.ProductionCooldown > 0) {
                    IssueActivityToMoveScout(scout, state, orders);
                } else if (scout.MovementCooldown <= 0) {

                    if (scout.PreviousCheckedLocation == scout.Actor.Location) {
                        IssueActivityToMoveScout(scout, state, orders);
                    } else {
                        // Scout has moved, so lets reset and check in on it next cooldown.
                        scout.PreviousCheckedLocation = scout.Actor.Location;
                        scout.MovementCooldown = ScoutActor.MOVEMENT_COOLDOWN_TICKS;
                    }
                }
            }
        }

        private void IssueActivityToMoveScout(ScoutActor scout, StrategicWorldState state, Queue<Order> orders)
        {
            scout.CurrentTargetLocation = targetPool.GetAvailableTargetLocation(state, scout.Actor);
            CPos target = scout.Actor.Trait<Mobile>().NearestMoveableCell(scout.CurrentTargetLocation);
            Order move = new Order("Move", scout.Actor, false) { TargetLocation = target};
            orders.Enqueue(move);

            scout.PreviousCheckedLocation = scout.Actor.Location;
            scout.MovementCooldown = ScoutActor.MOVEMENT_COOLDOWN_TICKS;
        }

        // ========================================
        // Scout Reporting
        // ========================================

        private void IssueScoutReports(StrategicWorldState state)
        {
            var actorsWhoCanReport = world.ActorsHavingTrait<RevealsShroud>().Where(a => a.Owner == selfPlayer && a.IsInWorld && !a.IsDead);
            foreach (Actor actor in actorsWhoCanReport) {
                ScoutReportInfoBuilder responseBuilder = ScoutReportUtils.BuildResponseInformationForActor(state, info, actor);
                if (responseBuilder == null) {
                    continue;
                }

                state.AddScoutReportInformation(actor, responseBuilder);
            }
        }
    }

    internal class ScoutActor
    {
        public const int MOVEMENT_COOLDOWN_TICKS = 100;

        public readonly Actor Actor;
        public int ProductionCooldown = 4;

        public CPos CurrentTargetLocation { get; set; }
        public CPos PreviousCheckedLocation { get; set; }
        public int MovementCooldown = 0;

        public ScoutActor(Actor actor)
        {
            this.Actor = actor;
            this.CurrentTargetLocation = CPos.Invalid;
            this.PreviousCheckedLocation = CPos.Invalid;
        }

        public bool HasTarget()
        {
            return (CurrentTargetLocation != CPos.Invalid);
        }
    }
}
