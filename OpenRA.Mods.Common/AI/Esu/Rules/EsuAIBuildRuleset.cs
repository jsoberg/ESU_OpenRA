using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRA.Traits;
using OpenRA.Support;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.AI.Esu.Geometry;

namespace OpenRA.Mods.Common.AI.Esu.Rules
{
    public class EsuAIBuildRuleset : BaseEsuAIRuleset
    {
        private EsuAIBuildHelper buildHelper;

        [Desc("Amount of ticks to wait after issuing a build order before we start analyzing rules again.")]
        private const int BUILDING_ORDER_COOLDOWN = 5;
        private int buildingOrderCooldown = 0;

        public EsuAIBuildRuleset(World world, EsuAIInfo info) : base(world, info)
        {
        }

        public override void Activate(Player selfPlayer)
        {
            base.Activate(selfPlayer);
            this.buildHelper = new EsuAIBuildHelper(world, selfPlayer, info);
        }

        public override void AddOrdersForTick(Actor self, StrategicWorldState state, Queue<Order> orders)
        {
            AddApplicableBuildRules(self, state, orders);
        }

        // ============================================
        // Build Rules
        // ============================================

        [Desc("Determines orders to be created from build rules.")]
        private void AddApplicableBuildRules(Actor self, StrategicWorldState state, Queue<Order> orders)
        {
            buildingOrderCooldown--;

            // No producers in world; this could be while we are building the construction yard, or if all yards have been destroyed.
            // TODO: Do we need this check, or will the build checks take care of it for us?
            if (!world.Actors.Any(a => a.Owner == selfPlayer && a.IsInWorld && !a.IsDead && a.TraitOrDefault<Production>() != null)) {
                return;
            }

            ExecuteBuildingRules(self, state, orders);
            ExecuteDefensiveBuildingRules(self, state, orders);

            // Place any completed buildings.
            buildHelper.PlaceBuildingsIfComplete(orders);
        }

        // ========================================
        // Buildings
        // ========================================

        private void ExecuteBuildingRules(Actor self, StrategicWorldState state, Queue<Order> orders)
        {
            // If a building is already being built or we're waiting for the cooldown, wait.
            if (EsuAIUtils.IsAnyItemCurrentlyInProductionForCategory(world, selfPlayer, EsuAIConstants.ProductionCategories.BUILDING)
                || buildingOrderCooldown > 0) {
                return;
            }

            Rule1_BuildPowerPlantIfBelowMinimumExcessPower(self, orders);
            Rule2_BuildOreRefineryIfApplicable(self, state, orders);
            Rule3_BuildOffensiveUnitProductionStructures(self, orders);
        }

        [Desc("Tunable rule: Build power plant if below X power.")]
        private void Rule1_BuildPowerPlantIfBelowMinimumExcessPower(Actor self, Queue<Order> orders)
        {
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
        private void Rule2_BuildOreRefineryIfApplicable(Actor self, StrategicWorldState state, Queue<Order> orders)
        {
            if (ShouldBuildRefinery(state)) {
                orders.Enqueue(Order.StartProduction(self, EsuAIConstants.Buildings.ORE_REFINERY, 1));
                buildingOrderCooldown = BUILDING_ORDER_COOLDOWN;
            }
        }

        private bool ShouldBuildRefinery(StrategicWorldState state)
        {
            // If ShouldProduceScoutBeforeRefinery is true and we don't yet have any scouts, we don't want to build a refinery yet.
            if (info.ShouldProduceScoutBeforeRefinery != 0 && !state.EnemyInfoList.Any(a => a.IsScouting)) {
                return false;
            }

            // Else, if we can and haven't yet met the minimum, then we should issue the build.
            var ownedActors = world.Actors.Where(a => a.Owner == selfPlayer && a.IsInWorld
                && !a.IsDead && a.TraitOrDefault<Refinery>() != null);
            return (ownedActors != null && ownedActors.Count() < 2);
        }

        private void Rule3_BuildOffensiveUnitProductionStructures(Actor self, Queue<Order> orders)
        {
            // TODO: Right now we just build barracks, obviously this needs to do something more.
            var ownedBarracks = EsuAIUtils.BuildingCountForPlayerOfType(world, selfPlayer, EsuAIConstants.Buildings.GetBarracksNameForPlayer(selfPlayer));
            if (ownedBarracks < 1) {
                orders.Enqueue(Order.StartProduction(self, EsuAIConstants.Buildings.GetBarracksNameForPlayer(selfPlayer), 1));
                buildingOrderCooldown = BUILDING_ORDER_COOLDOWN;
            }
        }

        // ========================================
        // Defensive Buildings
        // ========================================

        private void ExecuteDefensiveBuildingRules(Actor self, StrategicWorldState state, Queue<Order> orders)
        {
            // If a defensive building is already being built or we're waiting for the cooldown, wait.
            if (EsuAIUtils.IsAnyItemCurrentlyInProductionForCategory(world, selfPlayer, EsuAIConstants.ProductionCategories.DEFENSE)
                || buildingOrderCooldown > 0)
            {
                return;
            }

            Rule4_BuildDefensiveStructures(self, orders);
        }

        private void Rule4_BuildDefensiveStructures(Actor self, Queue<Order> orders)
        {
            double percentageSpentOnDefense;
            try {
                percentageSpentOnDefense = EsuAIUtils.GetPercentageOfResourcesSpentOnType(world, selfPlayer, EsuAIConstants.ProductionCategories.DEFENSE);
            } catch (NullReferenceException) {
                // Noting yet earned.
                return;
            }

            if (percentageSpentOnDefense < info.PercentageOfResourcesToSpendOnDefensiveBuildings) {

                var defenseiveBuilding = EsuAIConstants.Defense.GetRandomDefenseStructureForPlayer(selfPlayer);
                var queues = EsuAIUtils.FindProductionQueuesForPlayerAndCategory(world, selfPlayer, EsuAIConstants.ProductionCategories.DEFENSE);
                // TODO not first, decide where to go.
                var buildable = queues.First().AllItems().FirstOrDefault(a => a.Name == defenseiveBuilding);
                var prereqs = buildable.TraitInfo<BuildableInfo>().Prerequisites.Where(s => !s.StartsWith("~"));

                foreach (string req in prereqs) {
                    if (!EsuAIUtils.DoesItemCurrentlyExistOrIsBeingProducedForPlayer(world, selfPlayer, req)) {
                        // We need to build the prerequisite building first.
                        orders.Enqueue(Order.StartProduction(self, req, 1));
                        buildingOrderCooldown = BUILDING_ORDER_COOLDOWN;
                        return;
                    }
                }

                // We can build now.
                orders.Enqueue(Order.StartProduction(self, defenseiveBuilding, 1));
                buildingOrderCooldown = BUILDING_ORDER_COOLDOWN;
            }
        }
    }
}
