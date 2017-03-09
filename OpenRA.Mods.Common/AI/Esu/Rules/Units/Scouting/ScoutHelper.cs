using System.Collections.Generic;
using System.Linq;
using OpenRA.Traits;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.AI.Esu.Strategy;
using OpenRA.Mods.Common.AI.Esu.Strategy.Scouting;
using OpenRA.Mods.Common.AI.Esu.Rules.Units.Attacking;

namespace OpenRA.Mods.Common.AI.Esu.Rules.Units
{
    class ScoutHelper : INotifyDamage
    {
        private readonly World World;
        private readonly Player SelfPlayer;
        private readonly EsuAIInfo Info;

        private readonly List<ScoutActor> CurrentScouts;
        private readonly List<ScoutActor> DeadScouts;
        private readonly ScoutTargetLocationPool TargetPool;

        private string scoutInProductionName;

        public ScoutHelper(World world, Player selfPlayer, EsuAIInfo info)
        {
            this.World = world;
            this.SelfPlayer = selfPlayer;
            this.Info = info;

            this.CurrentScouts = new List<ScoutActor>();
            this.DeadScouts = new List<ScoutActor>();
            this.TargetPool = new ScoutTargetLocationPool(selfPlayer);

            // Add to callback list to get damage callbacks.
            DamageNotifier.AddDamageNotificationListener(this);
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
            CurrentScouts.Add(new ScoutActor(actor));
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
            if (scoutInProductionName != null || CurrentScouts.Count >= Info.NumberOfScoutsToProduce) {
                return false;
            }

            var productionQueues = EsuAIUtils.FindProductionQueuesForPlayerAndCategory(World, SelfPlayer, EsuAIConstants.ProductionCategories.INFANTRY);
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
            var productionQueues = EsuAIUtils.FindProductionQueuesForPlayerAndCategory(World, SelfPlayer, EsuAIConstants.ProductionCategories.INFANTRY);
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
            var availableActors = state.World.Actors.Where(a => a.Owner == SelfPlayer && !a.IsDead && a.Info.Name == scoutInProductionName 
                && !CurrentScouts.Any(sa => sa.Actor == a) && !state.ActiveAttackController.IsActorInvolvedInActiveAttack(a));

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
            if (CurrentScouts.Count() == 0) {
                return;
            }

            IssueMovementOrdersForScouts(state, orders);
            IssueScoutReports(state);
        }

        private void RemoveDeadScouts()
        {
            for (int i = (CurrentScouts.Count() - 1); i >= 0; i--) {
                ScoutActor scout = CurrentScouts[i];
                if (scout.Actor.IsDead) {
                    CurrentScouts.RemoveAt(i);
                    DeadScouts.Add(scout);
                }
            }
        }

        // ========================================
        // Scout Movement
        // ========================================

        private void IssueMovementOrdersForScouts(StrategicWorldState state, Queue<Order> orders)
        {
            foreach (ScoutActor scout in CurrentScouts) {
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
            scout.CurrentTargetLocation = TargetPool.GetAvailableTargetLocation(state, scout.Actor);
            CPos target = scout.Actor.Trait<Mobile>().NearestMoveableCell(scout.CurrentTargetLocation);
            Order move = new Order("Move", scout.Actor, false) { TargetLocation = target};
            orders.Enqueue(move);

            scout.PreviousCheckedLocation = scout.Actor.Location;
            scout.MovementCooldown = ScoutActor.MOVEMENT_COOLDOWN_TICKS;
        }

        // ========================================
        // Scout Reporting
        // ========================================

        private List<Actor> CachedEnemyActors;
        private readonly Queue<KillInfo> KilledActors = new Queue<KillInfo>();

        struct KillInfo
        {
            public Actor Killed;
            public Actor Attacker;  

            public KillInfo(Actor killed, Actor attacker)
            {
                this.Killed = killed;
                this.Attacker = attacker;
            }
        } 

        void INotifyDamage.Damaged(Actor attacked, AttackInfo e)
        {
            if (!attacked.IsDead || attacked.Owner != SelfPlayer || e.Attacker.Owner == SelfPlayer || attacked.TraitOrDefault<RevealsShroud>() == null || KilledActors.Any(k => k.Killed == attacked)) {
                return;
            }

            KillInfo kill = new KillInfo(attacked, e.Attacker);
            KilledActors.Enqueue(kill);
        }

        private void IssueScoutReports(StrategicWorldState state)
        {
            // Since finding enemy actors in the world is relatively taxing, 
            //     cache it here every 4 ticks and send it in when building response info.
            if (CachedEnemyActors == null || (state.World.GetCurrentLocalTickCount() % 4) == 0) {
                CachedEnemyActors = ScoutReportUtils.EnemyActorsInWorld(state, SelfPlayer);
            } else {
                CachedEnemyActors.RemoveAll(a => a.IsDead);
            }

            var actorsWhoCanReport = World.ActorsHavingTrait<RevealsShroud>().Where(a => a.Owner == SelfPlayer && a.IsInWorld && !a.IsDead);
            foreach (Actor actor in actorsWhoCanReport) {
                CacheResourceForPosition(state, actor.Location);

                ScoutReportInfoBuilder responseBuilder = ScoutReportUtils.BuildResponseInformationForActor(state, Info, actor, CachedEnemyActors);
                if (responseBuilder == null) {
                    continue;
                }

                state.AddScoutReportInformation(actor, responseBuilder);
            }

            while (KilledActors.Count > 0) {
                KillInfo kill = KilledActors.Dequeue();
                if (kill.Attacker.IsDead) {
                    continue;
                }

                ScoutReportInfoBuilder responseBuilder = ScoutReportUtils.BuildResponseInformationForActor(state, Info, kill.Killed, CachedEnemyActors, kill.Attacker);
                if (responseBuilder == null) {
                    continue;
                }
                state.AddScoutReportInformation(kill.Killed, responseBuilder);
            }
        }

        private void CacheResourceForPosition(StrategicWorldState state, CPos pos)
        {
            ResourceTile tile = state.World.Map.Resources[new MPos(pos.X, pos.Y)];
            if (tile.Type == 0 && tile.Index == 0) {
                return;
            }

            if (state.ResourceCache.ContainsKey(tile)) {
                state.ResourceCache[tile].Add(pos);
            } else {
                HashSet<CPos> positionSet = new HashSet<CPos>();
                positionSet.Add(pos);
                state.ResourceCache.Add(tile, positionSet);
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
