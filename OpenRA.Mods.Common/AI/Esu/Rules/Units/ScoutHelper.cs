using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRA.Traits;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.AI.Esu.Geometry;
using OpenRA.Activities;
using OpenRA.Mods.Common.Activities;
using OpenRA.Support;
using OpenRA.Mods.Common.AI.Esu.Strategy;

namespace OpenRA.Mods.Common.AI.Esu.Rules
{
    class ScoutHelper
    {
        private readonly MersenneTwister random;

        private readonly World world;
        private readonly Player selfPlayer;
        private readonly EsuAIInfo info;

        private readonly List<ScoutActor> currentScouts;
        private readonly List<ScoutActor> deadScouts;
        private string scoutInProductionName;

        public ScoutHelper(World world, Player selfPlayer, EsuAIInfo info)
        {
            this.random = new MersenneTwister();

            this.world = world;
            this.selfPlayer = selfPlayer;
            this.info = info;

            this.currentScouts = new List<ScoutActor>();
            this.deadScouts = new List<ScoutActor>();
        }

        public bool IsScoutBeingProduced()
        {
            return (scoutInProductionName != null);
        }

        // @return - returns true if the produced unit was claimed, false otherwise.
        public bool UnitProduced(Actor self, Actor other)
        {
            if (other.Info.Name == scoutInProductionName) {
                currentScouts.Add(new ScoutActor(other));
                scoutInProductionName = null;
                return true;
            }
            return false;
        }

        public void AddScoutOrdersIfApplicable(Actor self, StrategicWorldState state, Queue<Order> orders) 
        {
            IssueBuildScoutOrdersIfApplicable(self, state, orders);
            PerformCurrentScoutMaintenance(self, state, orders);
        }

        // ========================================
        // Scout Build Orders
        // ========================================

        private void IssueBuildScoutOrdersIfApplicable(Actor self, StrategicWorldState state, Queue<Order> orders)
        {
            if (!ShouldBuildNewScout(state)) {
                return;
            }

            AddBuildNewScoutOrder(self, orders);
        }

        private bool ShouldBuildNewScout(StrategicWorldState state)
        {
            if (scoutInProductionName != null || currentScouts.Count >= info.NumberOfScoutsToProduce || state.EnemyInfoList.All(a => a.FoundEnemyLocation != CPos.Invalid)) {
                return false;
            }

            var productionQueues = EsuAIUtils.FindProductionQueuesForPlayerAndCategory(world, selfPlayer, EsuAIConstants.ProductionCategories.INFANTRY);
            if (productionQueues.Count() == 0) {
                // We aren't able to build a scout right now.
                return false;
            }

            return true;
        }

        private void AddBuildNewScoutOrder(Actor self, Queue<Order> orders)
        {
            scoutInProductionName = GetBestAvailableScoutName();
            if (scoutInProductionName == null) {
                return;
            }

            orders.Enqueue(Order.StartProduction(self, scoutInProductionName, 1));
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

        // ========================================
        // Scout Movement/ Upkeep
        // ========================================

        private void PerformCurrentScoutMaintenance(Actor self, StrategicWorldState state, Queue<Order> orders)
        {
            RemoveDeadScouts();
            if (currentScouts.Count() == 0) {
                return;
            }

            IssueMovementOrdersForScouts(self, state, orders);
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

        private void IssueMovementOrdersForScouts(Actor self, StrategicWorldState state, Queue<Order> orders)
        {
            foreach (ScoutActor scout in currentScouts) {
                scout.ProductionCooldown--;
                scout.MovementCooldown--;

                if (scout.TargetLocation == CPos.Invalid && scout.ProductionCooldown > 0) {
                    CPos newScoutLoc = ChooseEnemyLocationForScout(scout, state);
                    scout.TargetLocation = newScoutLoc;
                    IssueActivityToMoveScout(scout, orders);
                } else if (scout.MovementCooldown <= 0) {

                    if (scout.TargetLocation != CPos.Invalid && scout.PreviousCheckedLocation == scout.Actor.Location) {
                        // Scout hasn't moved in awhile. Adjust the target location try to get it going.
                        CPos currentTarget = scout.TargetLocation;
                        scout.TargetLocation = world.Map.AllCells.Where(c => scout.Actor.Trait<Mobile>().CanMoveFreelyInto(c)).Random(random);
                        IssueActivityToMoveScout(scout, orders);
                    } else {
                        // Scout has moved, so lets reset and check in on it next cooldown.
                        scout.PreviousCheckedLocation = scout.Actor.Location;
                        scout.MovementCooldown = ScoutActor.MOVEMENT_COOLDOWN_TICKS;
                    }
                }
            }
        }

        private CPos ChooseEnemyLocationForScout(ScoutActor scout, StrategicWorldState state)
        {
            var enemy = state.EnemyInfoList.First();
            scout.EnemyName = enemy.EnemyName;
            // If the enemy isn't being scouted yet, return the predicted enemy location. Otherwise, get an unused corner.
            CPos location = enemy.PredictedEnemyLocation;// !enemy.IsScouting ? enemy.PredictedEnemyLocation : GetUnscoutedCorner(scout, state);
            enemy.IsScouting = true;
            return location;
        }

        private CPos GetUnscoutedCorner(ScoutActor scout, StrategicWorldState state)
        {
            var corners = GeometryUtils.GetMapCorners(world.Map);

            // Return first unused corner.
            foreach (CPos corner in corners) {
                if (currentScouts.All(s => s.TargetLocation != corner)) {
                    return corner;
                }
            }
            return GeometryUtils.OppositeCornerOfNearestCorner(world.Map, state.SelfIntialBaseLocation);
        }

        private void IssueActivityToMoveScout(ScoutActor scout, Queue<Order> orders)
        {
            CPos target = scout.Actor.Trait<Mobile>().NearestMoveableCell(scout.TargetLocation);
            Order move = new Order("Move", scout.Actor, false) { TargetLocation = target};
            orders.Enqueue(move);

            scout.PreviousCheckedLocation = scout.Actor.Location;
            scout.MovementCooldown = ScoutActor.MOVEMENT_COOLDOWN_TICKS;
        }
    }

    internal class ScoutActor
    {
        public const int MOVEMENT_COOLDOWN_TICKS = 100;

        public readonly Actor Actor;

        public string EnemyName { get; internal set; }
        public CPos TargetLocation { get; set; }
        public int ProductionCooldown = 4;

        public CPos PreviousCheckedLocation { get; set; }
        public int MovementCooldown = 0;

        public ScoutActor(Actor actor)
        {
            this.Actor = actor;
            this.TargetLocation = CPos.Invalid;
            this.PreviousCheckedLocation = CPos.Invalid;
        }
    }
}
