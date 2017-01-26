using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRA.Mods.Common.Traits;
using OpenRA.Support;
using OpenRA.Mods.Common.AI.Esu.Strategy;
using OpenRA.Mods.Common.AI.Esu.Strategy.Scouting;

namespace OpenRA.Mods.Common.AI.Esu.Rules.Units
{
    public class UnitProductionHelper
    {
        private const double DefaultInfantryPercentage = .85;
        private const double DefaultVehiclePercentage = .15;

        private static MersenneTwister RANDOM = new MersenneTwister(); 

        private const int UNIT_PRODUCTION_COOLDOWN = 10;
        private int UnitProductionCheckCooldown;

        private readonly World world;
        private readonly Player selfPlayer;
        private readonly EsuAIInfo info;

        private readonly CompiledUnitDamageStatisticsLoader UnitStatsLoader;

        public UnitProductionHelper(World world, Player selfPlayer, EsuAIInfo info)
        {
            this.world = world;
            this.selfPlayer = selfPlayer;
            this.info = info;

            this.UnitStatsLoader = new CompiledUnitDamageStatisticsLoader();
            UnitStatsLoader.ReloadUnitDamageStats();
        }

        public void OnOrderDenied(Order order)
        {
            /* Do Nothing. */
        }

        public void AddUnitOrdersIfApplicable(StrategicWorldState state, Queue<Order> orders)
        {
            UnitProductionCheckCooldown --;
            if (UnitProductionCheckCooldown > 0) {
                return;
            }

            ChooseAndProduceUnit(state, orders);
        }

        private void ChooseAndProduceUnit(StrategicWorldState state, Queue<Order> orders)
        {
            // Give harvesters precedence.
            if (ProduceHarvesterIfApplicable(state, orders))
            {
                return;
            }

            // TODO We don't necessarily want the best fit cell.
            AggregateScoutReportData data = state.ScoutReportGrid.GetCurrentBestFitCell();
            if (data == null) {
                ProduceUnitForDistribution(state, orders, DefaultInfantryPercentage, DefaultVehiclePercentage, 0d);
            }

            // TODO : debug code (we want to base this off of the aggregate info).
            ProduceUnitForDistribution(state, orders, DefaultInfantryPercentage, DefaultVehiclePercentage, 0d);
        }

        /** @return true if a harvester production order was issued this call, false otherwise. */
        private bool ProduceHarvesterIfApplicable(StrategicWorldState state, Queue<Order> orders)
        {
            if (EsuAIUtils.IsItemCurrentlyInProductionForCategory(state.World, selfPlayer, EsuAIConstants.ProductionCategories.VEHICLE, EsuAIConstants.Vehicles.HARVESTER) ||
                    EsuAIUtils.IsItemCurrentlyInProductionForCategory(state.World, selfPlayer, EsuAIConstants.ProductionCategories.BUILDING, EsuAIConstants.Buildings.WAR_FACTORY))
            {
                return false;
            }

            if (NumOreRefineriesBuilt(state) < info.MinNumRefineries)
            {
                return false;
            }

            if (NumLivingHarvesters(state) < info.MinNumHarvesters)
            {
                ProduceVehicle(state, orders, EsuAIConstants.Vehicles.HARVESTER);
                return true;
            }

            return false;
        }

        private int NumOreRefineriesBuilt(StrategicWorldState state)
        {
            return state.World.Actors.Count(a => a.Owner == selfPlayer && a.IsInWorld
                && !a.IsDead && a.TraitOrDefault<Refinery>() != null);
        }

        private int NumLivingHarvesters(StrategicWorldState state)
        {
            return state.World.ActorsHavingTrait<Harvester>().Count(a => a.Owner == selfPlayer && !a.IsDead);
        }

        // Produces units for the specified distribution. If we currently have excess resources, we will produce units without taking the distribution into account.
        private void ProduceUnitForDistribution(StrategicWorldState state, Queue<Order> orders, double infantryPercent, double vehiclePercent, double airPercent)
        {
            double currentResources = EsuAIUtils.GetCurrentResourcesForPlayer(selfPlayer);

            // TODO: Check here for vehicle and air.
            double currentInfantryPercentage = PercentageOfSelfOffensiveUnitsCurrentlyInWorldOfType(EsuAIConstants.ProductionCategories.INFANTRY);
            if (currentInfantryPercentage < infantryPercent || currentResources > info.ExcessResourceLevel)
            {
                ProduceInfantry(state, orders);
            }

            double currentVehiclePercentage = PercentageOfSelfOffensiveUnitsCurrentlyInWorldOfType(EsuAIConstants.ProductionCategories.VEHICLE);
            if (currentVehiclePercentage < vehiclePercent || currentResources > info.ExcessResourceLevel)
            {
                var vehicleName = GetVehicleToProduce();
                ProduceVehicle(state, orders, vehicleName);
            }
        }

        private double PercentageOfSelfOffensiveUnitsCurrentlyInWorldOfType(string type)
        {
            // Don't include harvesters (not offense)
            var actors = world.Actors.Where(a => a.Owner == selfPlayer && !a.IsDead && EsuAIUtils.IsActorOfType(world, a, type) && a.Info.Name != "harv");
            // TODO yech... find a better way to get this.
            // All offensive actors.
            var allActors = world.Actors.Where(a => a.Owner == selfPlayer && !a.IsDead && a.Info.Name != "harv" && !EsuAIUtils.IsActorOfType(world, a, EsuAIConstants.ProductionCategories.BUILDING)
                && !EsuAIUtils.IsActorOfType(world, a, EsuAIConstants.ProductionCategories.DEFENSE));

            if (allActors.Count() == 0) {
                return 0;
            }
            return ((double) actors.Count() / (double) allActors.Count());
        }

        private void ProduceInfantry(StrategicWorldState state, Queue<Order> orders)
        {
            if (EsuAIUtils.IsAnyItemCurrentlyInProductionForCategory(world, selfPlayer, EsuAIConstants.ProductionCategories.INFANTRY)) {
                return;
            }

            var infantry = GetInfantryToProduce();


            bool wasOrderIssued = ProduceUnit(state, orders, infantry, EsuAIConstants.ProductionCategories.INFANTRY);
            if (!wasOrderIssued) {
                ScheduleBuildingProduction(EsuAIConstants.Buildings.GetBarracksNameForPlayer(selfPlayer), state, orders);
            }
            UnitProductionCheckCooldown = UNIT_PRODUCTION_COOLDOWN;
        }

        private string GetInfantryToProduce()
        {
            Dictionary<string, DamageKillStats> infantryStats = UnitStatsLoader.GetStatsForActors(EsuAIConstants.Infantry.AVAILABLE_WITH_BARRACKS);
            if (infantryStats == null || ShouldProduceRandomUnit()) {
                return EsuAIConstants.Infantry.AVAILABLE_WITH_BARRACKS.Random(RANDOM);
            } else {
                return GetUnitForStats(infantryStats);
            }
        }

        private bool ShouldProduceRandomUnit()
        {
            float chooseRandom = RANDOM.NextFloat();
            return chooseRandom <= info.GetUnitProductionRandomPercentage();
        }

        private string GetUnitForStats(Dictionary<string, DamageKillStats> stats)
        {
            double totalDamage = 0;
            foreach (DamageKillStats stat in stats.Values) {
                totalDamage += stat.Damage;
            }

            Dictionary<float, string> percentDamageToUnit = new Dictionary<float, string>();
            foreach (KeyValuePair<string, DamageKillStats> stat in stats) {
                percentDamageToUnit.Add((float) (stat.Value.Damage / totalDamage), stat.Key);
            }

            var sorted = from entry in percentDamageToUnit orderby entry.Value descending select entry;
            float val = RANDOM.NextFloat();
            float current = 0;
            foreach (KeyValuePair<float, string> entry in sorted)
            {
                current += entry.Key;
                if (val <= current) {
                    return entry.Value;
                }
            }
            return null;
        }

        private void ProduceVehicle(StrategicWorldState state, Queue<Order> orders, string vehicleName)
        {
            if (EsuAIUtils.IsAnyItemCurrentlyInProductionForCategory(world, selfPlayer, EsuAIConstants.ProductionCategories.VEHICLE)) {
                return;
            }

            bool wasOrderIssued = ProduceUnit(state, orders, vehicleName, EsuAIConstants.ProductionCategories.VEHICLE);
            if (!wasOrderIssued) {
                ScheduleBuildingProduction(EsuAIConstants.Buildings.WAR_FACTORY, state, orders);
            }
            UnitProductionCheckCooldown = UNIT_PRODUCTION_COOLDOWN;
        }

        private string GetVehicleToProduce()
        {
            Dictionary<string, DamageKillStats> vehicleStats = UnitStatsLoader.GetStatsForActors(EsuAIConstants.Vehicles.GetVehiclesForPlayer(selfPlayer));
            if (vehicleStats == null || ShouldProduceRandomUnit()) {
                return EsuAIConstants.Vehicles.GetRandomVehicleForPlayer(selfPlayer);
            } else {
                return GetUnitForStats(vehicleStats);
            }
        }

        private bool ProduceUnit(StrategicWorldState state, Queue<Order> orders, string unitName, string productionCategory)
        {
            var queues = EsuAIUtils.FindProductionQueuesForPlayerAndCategory(world, selfPlayer, productionCategory);

            var buildable = queues.First().AllItems().FirstOrDefault(a => a.Name == unitName);
            if (buildable == null) {
                return false;
            }

            var prereqs = buildable.TraitInfo<BuildableInfo>().Prerequisites.Where(s => !s.StartsWith("~"));
            foreach (string req in prereqs) {
                if (ScheduleBuildingProduction(req, state, orders)) {
                    // We have scheduled construction of this building.
                    return true;
                }
            }

            // We can build now.
            orders.Enqueue(Order.StartProduction(selfPlayer.PlayerActor, unitName, 1));
            return false;
        }

        /** @return - true if the building production has been scheduled, false otherwise. */
        private bool ScheduleBuildingProduction(string building, StrategicWorldState state, Queue<Order> orders)
        {
            if (!state.RequestedBuildingQueue.Contains(building)
                    && !EsuAIUtils.DoesItemCurrentlyExistOrIsBeingProducedForPlayer(world, selfPlayer, building)) 
            {
                state.RequestedBuildingQueue.Enqueue(building);
                UnitProductionCheckCooldown = UNIT_PRODUCTION_COOLDOWN;
                return true;
            }
            return false;
        }
    }
}
