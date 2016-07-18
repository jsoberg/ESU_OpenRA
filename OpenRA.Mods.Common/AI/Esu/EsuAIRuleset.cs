using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRA.Traits;
using OpenRA.Support;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.AI.Esu.Geometry;

namespace OpenRA.Mods.Common.AI.Esu
{
    class EsuAIRuleset
    {
        private readonly World world;
        private readonly EsuAIInfo info;

        private Player selfPlayer;

        public EsuAIRuleset(World world, EsuAIInfo info)
        {
            this.world = world;
            this.info = info;
        }

        public void Activate(Player selfPlayer)
        {
            this.selfPlayer = selfPlayer;
        }

        public IEnumerable<Order> Tick(Actor self)
        {
            Queue<Order> orders = new Queue<Order>();

            AddApplicableBuildRules(self, orders);

            return orders;
        }

        // ============================================
        // Build Rules
        // ============================================

         [Desc("Amount of ticks to wait after issuing a build order before we start analyzing rules again.")]
        private const int BUILDING_ORDER_COOLDOWN = 5;

        private int buildingOrderCooldown = 0;

        [Desc("Determines orders to be created from build rules.")]
        private void AddApplicableBuildRules(Actor self, Queue<Order> orders)
        {
            buildingOrderCooldown--;

            // No producers in world; this could be while we are building the construction yard, or if all yards have been destroyed.
            // TODO: Do we need this check, or will the build checks take care of it for us?
            if (!world.Actors.Any(a => a.Owner == selfPlayer && a.IsInWorld && !a.IsDead && a.TraitOrDefault<Production>() != null)) {
                return;
            }

            // Building is already being built, so try to place any finished buildings and wait until building queue is available.
            if (EsuAIUtils.IsAnyItemCurrentlyInProductionForCategory(world, selfPlayer, EsuAIConstants.ProductionCategories.BUILDING)
                || buildingOrderCooldown > 0) {
                PlaceBuildingsIfComplete(orders);
                return;
            }

            // Build rules.
            {
                Rule1_BuildPowerPlantIfBelowMinimumExcessPower(self, orders);
                Rule2_BuildOreRefineryIfApplicable(self, orders);
            }

            // Place completed buildings.
            PlaceBuildingsIfComplete(orders);
        }

        [Desc("Tunable rule: Build power plant if below X power.")]
        private void Rule1_BuildPowerPlantIfBelowMinimumExcessPower(Actor self, Queue<Order> orders)
        {
            // Return if we can't build a power plant or we're already building a power plant.
            if (!EsuAIUtils.CanBuildItemWithNameForCategory(world, selfPlayer, EsuAIConstants.ProductionCategories.BUILDING, EsuAIConstants.Buildings.POWER_PLANT)) {
                return;
            }

            PowerManager pm = self.Trait<PowerManager>();

            if (pm.ExcessPower < info.MinimumExcessPower) {
                orders.Enqueue(Order.StartProduction(self, EsuAIConstants.Buildings.POWER_PLANT, 1));
                buildingOrderCooldown = BUILDING_ORDER_COOLDOWN;
            }
        }

        [Desc("Adds order to place building if any buildings are complete.")]
        private void PlaceBuildingsIfComplete(Queue<Order> orders)
        {
            IEnumerable<ProductionQueue> productionQueues = EsuAIUtils.FindProductionQueues(world, selfPlayer, EsuAIConstants.ProductionCategories.BUILDING);
            foreach (ProductionQueue queue in productionQueues) {
                var currentBuilding = queue.CurrentItem();
                if (currentBuilding != null && currentBuilding.Done) {

                    // TODO: This code is also from HackyAI, but will have to do for now.
                    var type = BuildingType.Building;
                    if (world.Map.Rules.Actors[currentBuilding.Item].HasTraitInfo<AttackBaseInfo>())
                        type = BuildingType.Defense;
                    else if (world.Map.Rules.Actors[currentBuilding.Item].HasTraitInfo<RefineryInfo>())
                        type = BuildingType.Refinery;

                   var location = FindBuildLocation(currentBuilding.Item, type);

                    // TODO: handle null location found (cancel production?)
                   if (location != null) {
                       orders.Enqueue(new Order("PlaceBuilding", selfPlayer.PlayerActor, false)
                       {
                           TargetLocation = location.Value,
                           TargetString = currentBuilding.Item,
                           TargetActor = queue.Actor,
                           SuppressVisualFeedback = true
                       });
                   }
                }
            }
        }

        public CPos? FindBuildLocation(string actorType, BuildingType type)
        {
            var bi = world.Map.Rules.Actors[actorType].TraitInfoOrDefault<BuildingInfo>();
            if (bi == null)
                return null;

            // Find the buildable cell that is closest to pos and centered around center
            Func<CPos, int, int, CPos?> findPos = (center, minRange, maxRange) =>
            {
                var cells = world.Map.FindTilesInAnnulus(center, minRange, maxRange);

                foreach (var cell in cells) {
                    if (!world.CanPlaceBuilding(actorType, bi, cell, null))
                        continue;
                    if (!bi.IsCloseEnoughToBase(world, selfPlayer, actorType, cell))
                        continue;

                    return cell;
                }
                return null;
            };

            var baseCenter = GetRandomBaseCenter();
            switch (type) {
                case BuildingType.Defense:
                    // TODO find optimal placement.
                case BuildingType.Refinery:
                    // TODO find placement nearest to resources.
                case BuildingType.Building:
                    return findPos(baseCenter, 0, world.Map.Grid.MaximumTileSearchRange);
            }

            // Can't find a build location
            return null;
        }

        public readonly MersenneTwister Random = new MersenneTwister();

        // TODO: This was copied from HackyAI; We want to be smarter about this than 
        // just building at a random construction yard, but this will do for now.
        private CPos GetRandomBaseCenter()
        {
            var randomConstructionYard = world.Actors.Where(a => a.Owner == selfPlayer &&
                a.Info.Name == EsuAIConstants.Buildings.CONSTRUCTION_YARD)
                .RandomOrDefault(Random);

            // TODO: Possible NPE
            return randomConstructionYard.Location;
        }

        private void Rule2_BuildOreRefineryIfApplicable(Actor self, Queue<Order> orders)
        {
            if (EsuAIUtils.IsItemCurrentlyInProductionForCategory(world, selfPlayer, EsuAIConstants.ProductionCategories.BUILDING, EsuAIConstants.Buildings.ORE_REFINERY)) {
                return;
            }

            // Static portion of rule: we need at least two ore refineries.
            var ownedActors = world.Actors.Where(a => a.Owner == selfPlayer && a.IsInWorld
                && !a.IsDead && a.TraitOrDefault<Refinery>() != null);

            if (ownedActors != null && ownedActors.Count() < 2) {
                orders.Enqueue(Order.StartProduction(self, EsuAIConstants.Buildings.ORE_REFINERY, 1));
                buildingOrderCooldown = BUILDING_ORDER_COOLDOWN;
            }

            // Tunable portion of rule: TBD
        }
    }
}
