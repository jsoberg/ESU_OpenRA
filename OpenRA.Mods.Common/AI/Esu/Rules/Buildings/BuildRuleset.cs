using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.AI.Esu.Strategy;
using OpenRA.Mods.Common.AI.Esu.Rules.Units;

namespace OpenRA.Mods.Common.AI.Esu.Rules.Buildings
{
    public class BuildRuleset : BaseEsuAIRuleset
    {
        private BuildHelper buildHelper;

        [Desc("Amount of ticks to wait after issuing a build order before we start analyzing rules again.")]
        private const int BUILDING_ORDER_COOLDOWN = 10;

        private int buildingOrderCooldown = 0;
        private int DefensiveOrderCheckCooldown = 0;

        private bool wasBuildingOrderIssuedThisTick;

        public BuildRuleset(World world, EsuAIInfo info) : base(world, info)
        {
        }

        public override void Activate(Player selfPlayer)
        {
            base.Activate(selfPlayer);
            this.buildHelper = new BuildHelper(world, selfPlayer, info);
        }

        public override void AddOrdersForTick(Actor self, StrategicWorldState state, Queue<Order> orders)
        {
            wasBuildingOrderIssuedThisTick = false;

            AddApplicableBuildRules(self, state, orders);
        }

        // ============================================
        // Build Rules
        // ============================================

        [Desc("Determines orders to be created from build rules.")]
        private void AddApplicableBuildRules(Actor self, StrategicWorldState state, Queue<Order> orders)
        {
            buildingOrderCooldown--;
            DefensiveOrderCheckCooldown--;

            // No producers in world; this could be while we are building the construction yard, or if all yards have been destroyed.
            // TODO: Do we need this check, or will the build checks take care of it for us?
            if (!world.Actors.Any(a => a.Owner == selfPlayer && a.IsInWorld && !a.IsDead && a.TraitOrDefault<Production>() != null)) {
                return;
            }

            ExecuteBuildingRules(self, state, orders);
            ExecuteDefensiveBuildingRules(self, state, orders);

            // Place any completed buildings.
            buildHelper.PlaceBuildingsIfComplete(state, orders);
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

            // Issue build orders in order of importance.
            try {
                // Most important: Power Plants
                BuildPowerPlantIfBelowMinimumExcessPower(self, orders);
                AssertProductionOrderNotIssuedThisTick();

                // Second most important: Ore Refineries
                BuildOreRefineryIfApplicable(self, state, orders);
                AssertProductionOrderNotIssuedThisTick();

                // Third most important: Offensive Unit Structures.
                BuildOffensiveUnitProductionStructures(self, orders);
                AssertProductionOrderNotIssuedThisTick();

                // Least important: any queued builds.
                BuildQueuedBuildings(self, state, orders);
            } catch (ProductionOrderIssuedException) {
                // Do nothing, we just want to continue here.
            }
            
        }

        [Desc("Tunable rule: Build power plant if below X power.")]
        private void BuildPowerPlantIfBelowMinimumExcessPower(Actor self, Queue<Order> orders)
        {
            if (!EsuAIUtils.CanBuildItemWithNameForCategory(world, selfPlayer, EsuAIConstants.ProductionCategories.BUILDING, EsuAIConstants.Buildings.POWER_PLANT)) {
                return;
            }

            PowerManager pm = self.Trait<PowerManager>();
            if (pm.ExcessPower < info.MinimumExcessPower) {
                StartProduction(self, orders, EsuAIConstants.Buildings.POWER_PLANT);
            }
        }

        // TODO: Tunable portion incomplete.
        private void BuildOreRefineryIfApplicable(Actor self, StrategicWorldState state, Queue<Order> orders)
        {
            if (ShouldBuildRefinery(state)) {
                StartProduction(self, orders, EsuAIConstants.Buildings.ORE_REFINERY);
            }
        }

        private bool ShouldBuildRefinery(StrategicWorldState state)
        {
            // If ShouldProduceScoutBeforeRefinery is true and we don't yet have any scouts, we don't want to build a refinery yet.
            if (info.ShouldProduceScoutBeforeRefinery != 0 && NumOwnedBarracks() < 1) {
                return false;
            }

            // Else, if we can and haven't yet met the minimum, then we should issue the build.
            var ownedActors = world.Actors.Where(a => a.Owner == selfPlayer && a.IsInWorld
                && !a.IsDead && a.TraitOrDefault<Refinery>() != null);
            return (ownedActors != null && ownedActors.Count() < info.MinNumRefineries);
        }

        private void BuildOffensiveUnitProductionStructures(Actor self, Queue<Order> orders)
        {
            // TODO: Right now we just build barracks, obviously this needs to do something more.
            if (NumOwnedBarracks() < 1) {
                StartProduction(self, orders, EsuAIConstants.Buildings.GetBarracksNameForPlayer(selfPlayer));
            }
        }

        private int NumOwnedBarracks()
        {
            return EsuAIUtils.BuildingCountForPlayerOfType(world, selfPlayer, EsuAIConstants.Buildings.GetBarracksNameForPlayer(selfPlayer));
        }

        private void BuildQueuedBuildings(Actor self, StrategicWorldState state, Queue<Order> orders)
        {
            if (state.RequestedBuildingQueue.Count > 0) {
                string front = state.RequestedBuildingQueue.Dequeue();
                if (!EsuAIUtils.DoesItemCurrentlyExistOrIsBeingProducedForPlayer(world, selfPlayer, front)) {
                    StartProduction(self, orders, front);
                }
            }
        }

        private void StartProduction(Actor self, Queue<Order> orders, string buildingName)
        {
            orders.Enqueue(Order.StartProduction(self, buildingName, 1));
            buildingOrderCooldown = BUILDING_ORDER_COOLDOWN;
            wasBuildingOrderIssuedThisTick = true;
        }

        // Throws a ProductionOrderIssuedException if a production order was issued this tick.
        private void AssertProductionOrderNotIssuedThisTick()
        {
            if (wasBuildingOrderIssuedThisTick) {
                throw new ProductionOrderIssuedException();
            }
        }

        private class ProductionOrderIssuedException : Exception { }

        // ========================================
        // Defensive Buildings
        // ========================================

        private void ExecuteDefensiveBuildingRules(Actor self, StrategicWorldState state, Queue<Order> orders)
        {
            // If a defensive building is already being built or we're waiting for the cooldown, wait.
            if (EsuAIUtils.IsAnyItemCurrentlyInProductionForCategory(world, selfPlayer, EsuAIConstants.ProductionCategories.DEFENSE)
                || DefensiveOrderCheckCooldown > 0) 
            {
                return;
            }

            BuildDefensiveStructures(self, state, orders);
                            DefensiveOrderCheckCooldown = BUILDING_ORDER_COOLDOWN;
        }

        private void BuildDefensiveStructures(Actor self, StrategicWorldState state, Queue<Order> orders)
        {
            double percentageSpentOnDefense = EsuAIUtils.GetPercentageOfResourcesSpentOnProductionType(world, selfPlayer, EsuAIConstants.ProductionCategories.DEFENSE);
            if (percentageSpentOnDefense < 0) {
                return;
            }

            double percentageToSpend = (info.PercentageOfResourcesToSpendOnDefensiveBuildings / 100.0);
            if (percentageSpentOnDefense < percentageToSpend) {

                var defenseiveBuilding = GetDefensiveStructureToProduce(state);
                var queues = EsuAIUtils.FindProductionQueuesForPlayerAndCategory(world, selfPlayer, EsuAIConstants.ProductionCategories.DEFENSE);
                // TODO not first, decide where to go.
                var buildable = queues.First().AllItems().FirstOrDefault(a => a.Name == defenseiveBuilding);
                if (buildable == null) {
                    // We have no construction yard.
                    return;
                }

                var prereqs = buildable.TraitInfo<BuildableInfo>().Prerequisites.Where(s => !s.StartsWith("~"));

                foreach (string req in prereqs) {
                    if (!EsuAIUtils.DoesItemCurrentlyExistOrIsBeingProducedForPlayer(world, selfPlayer, req)) {
                        // We need to build the prerequisite building first, so queue it up.
                        state.RequestedBuildingQueue.Enqueue(req);
                        return;
                    }
                }

                // We can build now.
                orders.Enqueue(Order.StartProduction(self, defenseiveBuilding, 1));
            }
        }

        private string GetDefensiveStructureToProduce(StrategicWorldState state)
        {
            Dictionary<string, DamageKillStats> defenseStats = state.UnitStatsLoader.GetStatsForActors(EsuAIConstants.Defense.GetDefenseStructuresForPlayer(selfPlayer));
            if (defenseStats == null) {
                return EsuAIConstants.Defense.GetRandomDefenseStructureForPlayer(selfPlayer);
            } else {
                return state.UnitStatsLoader.GetUnitForStats(defenseStats);
            }
        }
    }
}
