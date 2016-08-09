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

namespace OpenRA.Mods.Common.AI.Esu.Rules
{
    class EsuAIScoutHelper
    {
        private readonly MersenneTwister random;

        private readonly World world;
        private readonly Player selfPlayer;
        private readonly EsuAIInfo info;

        private readonly List<ScoutActor> currentScouts;
        private readonly List<ScoutActor> deadScouts;
        private string scoutInProductionName;

        public EsuAIScoutHelper(World world, Player selfPlayer, EsuAIInfo info)
        {
            this.random = new MersenneTwister();

            this.world = world;
            this.selfPlayer = selfPlayer;
            this.info = info;

            this.currentScouts = new List<ScoutActor>();
            this.deadScouts = new List<ScoutActor>();
        }

        public void UnitProduced(Actor self, Actor other)
        {
            if (other.Info.Name == scoutInProductionName) {
                currentScouts.Add(new ScoutActor(other));
                scoutInProductionName = null;
            }
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
            // TODO: If a scout dies without finding enemy location, IsScouting is still true.
            if (scoutInProductionName != null || currentScouts.Count > 0 || state.EnemyInfoList.All(a => a.IsScouting)) {
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
                    IssueActivityToMoveScout(scout);
                } else if (scout.MovementCooldown <= 0) {

                    if (scout.TargetLocation != CPos.Invalid && scout.PreviousCheckedLocation == scout.Actor.Location) {
                        // Scout hasn't moved in awhile. Adjust the target location try to get it going.
                        CPos currentTarget = scout.TargetLocation;
                        CPos adjustedTarget = world.Map.AllCells.Where(c => scout.Actor.Trait<Mobile>().CanMoveFreelyInto(c)).Random(random);
                        scout.TargetLocation = scout.Actor.Trait<Mobile>().NearestMoveableCell(adjustedTarget);
                        IssueActivityToMoveScout(scout);
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
            var enemy = state.EnemyInfoList.First(a => !a.IsScouting);
            enemy.IsScouting = true;
            scout.EnemyName = enemy.EnemyName;
            CPos targetLocation = enemy.PredictedEnemyLocation;

            return scout.Actor.Trait<Mobile>().NearestMoveableCell(targetLocation);
        }

        private void IssueActivityToMoveScout(ScoutActor scout)
        {
            Target moveTarget = Target.FromCell(world, scout.TargetLocation);
            Activity move = scout.Actor.Trait<IMove>().MoveToTarget(scout.Actor, moveTarget);
            scout.Actor.CancelActivity();
            scout.Actor.QueueActivity(move);

            scout.PreviousCheckedLocation = scout.Actor.Location;
            scout.MovementCooldown = ScoutActor.MOVEMENT_COOLDOWN_TICKS;
        }

        private CPos OppositeCornerOfNearestCorner(CPos currentLoc)
        {
            var width = world.Map.MapSize.X;
            var height = world.Map.MapSize.Y;

            var topLeft = new CPos(0, 0);
            var topRight = new CPos(width, 0);
            var botLeft = new CPos(0, height);
            var botRight = new CPos(width, height);

            // Opposite corner will be farthest away.
            CPos[] corners = new CPos[] { topLeft, topRight, botLeft, botRight };
            int largestDistIndex = 0;
            double largestDist = double.MinValue;
            for (int i = 0; i < corners.Count(); i ++) {
                double dist = GeometryUtils.EuclideanDistance(currentLoc, corners[i]);
                if (dist > largestDist) {
                    largestDistIndex = i;
                    largestDist = dist;
                }
            }

            return corners[largestDistIndex];
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
