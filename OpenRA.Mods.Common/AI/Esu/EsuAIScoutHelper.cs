﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRA.Traits;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.AI.Esu.Geometry;
using OpenRA.Activities;
using OpenRA.Mods.Common.Activities;

namespace OpenRA.Mods.Common.AI.Esu
{
    class EsuAIScoutHelper
    {
        private readonly World world;
        private readonly Player selfPlayer;
        private readonly EsuAIInfo info;

        private readonly List<ScoutActor> currentScouts;
        private readonly List<ScoutActor> deadScouts;
        private string scoutInProductionName;

        public EsuAIScoutHelper(World world, Player selfPlayer, EsuAIInfo info)
        {
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

        public void AddScoutOrdersIfApplicable(Actor self, Queue<Order> orders) 
        {
            IssueBuildScoutOrdersIfApplicable(self, orders);
            PerformCurrentScoutMaintenance(self, orders);
        }

        // ========================================
        // Scout Build Orders
        // ========================================

        private void IssueBuildScoutOrdersIfApplicable(Actor self, Queue<Order> orders)
        {
            if (!ShouldBuildNewScout()) {
                return;
            }

            AddBuildNewScoutOrder(self, orders);
        }

        private bool ShouldBuildNewScout()
        {
            // TODO: We might want more than one scout, so the second half of this check might not make sense.
            if (scoutInProductionName != null || currentScouts.Count > 0) {
                return false;
            }

            var productionQueues = EsuAIUtils.FindProductionQueues(world, selfPlayer, EsuAIConstants.ProductionCategories.INFANTRY);
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
            var productionQueues = EsuAIUtils.FindProductionQueues(world, selfPlayer, EsuAIConstants.ProductionCategories.INFANTRY);
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

        private void PerformCurrentScoutMaintenance(Actor self, Queue<Order> orders)
        {
            RemoveDeadScouts();
            if (currentScouts.Count() == 0) {
                return;
            }

            IssueMovementOrdersForScouts(self, orders);
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

        private void IssueMovementOrdersForScouts(Actor self, Queue<Order> orders)
        {
            foreach (ScoutActor scout in currentScouts) {
                scout.ProductionCooldown--;
                if (scout.TargetLocation == CPos.Invalid && scout.ProductionCooldown > 0) {
                    CPos targetLocation = GetNewTargetLocationForScout(scout);

                    scout.TargetLocation = scout.Actor.Trait<Mobile>().NearestMoveableCell(targetLocation);
                    Target moveTarget = Target.FromCell(world, scout.TargetLocation);
                    Activity move = new MoveAdjacentTo(scout.Actor, moveTarget);
                    scout.Actor.QueueActivity(move);
                }
            }
        }

        private CPos GetNewTargetLocationForScout(ScoutActor scout)
        {
            // TODO right now just return opposite of construction yard; this can be improved.
            return PredictedEnemyLocation();
        }

        private CPos PredictedEnemyLocation()
        {
            var constructionYard = world.Actors.Where(a => a.Owner == selfPlayer &&
                a.Info.Name == EsuAIConstants.Buildings.CONSTRUCTION_YARD).FirstOrDefault();
            var selfLocation = constructionYard.Location;

            var width = world.Map.MapSize.X;
            var height = world.Map.MapSize.Y;

            var enemyX = width - selfLocation.X;
            var enemyY = height - selfLocation.Y;

            return new CPos(enemyX, enemyY);
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
        public readonly Actor Actor;

        public CPos TargetLocation { get; set; }
        public int ProductionCooldown = 4;

        public ScoutActor(Actor actor)
        {
            this.Actor = actor;
            this.TargetLocation = CPos.Invalid;
        }
    }
}
