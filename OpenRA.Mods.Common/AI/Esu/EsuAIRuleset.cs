using System;
using System.Collections;
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
        private EsuAIBuildHelper buildHelper;

        [Desc("Amount of ticks to wait after issuing a build order before we start analyzing rules again.")]
        private const int BUILDING_ORDER_COOLDOWN = 5;
        private int buildingOrderCooldown = 0;

        public EsuAIRuleset(World world, EsuAIInfo info)
        {
            this.world = world;
            this.info = info;
        }

        public void Activate(Player selfPlayer)
        {
            this.selfPlayer = selfPlayer;
            this.buildHelper = new EsuAIBuildHelper(world, selfPlayer, info);
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
                buildHelper.PlaceBuildingsIfComplete(orders);
                return;
            }

            // Build rules.
            {
                Rule1_BuildPowerPlantIfBelowMinimumExcessPower(self, orders);
                Rule2_BuildOreRefineryIfApplicable(self, orders);
            }

            // Place completed buildings.
            buildHelper.PlaceBuildingsIfComplete(orders);
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

        // TODO: Tunable portion incomplete.
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
