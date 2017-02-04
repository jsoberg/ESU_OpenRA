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

        private readonly World World;
        private readonly Player SelfPlayer;
        private readonly EsuAIInfo Info;

        private readonly List<Actor> OffensiveActorsCache;

        public UnitProductionHelper(World world, Player selfPlayer, EsuAIInfo info)
        {
            this.World = world;
            this.SelfPlayer = selfPlayer;
            this.Info = info;

            this.OffensiveActorsCache = new List<Actor>();
        }

        public void OnOrderDenied(Order order)
        {
            /* Do Nothing. */
        }

        public void UnitProduced(Actor producer, Actor produced)
        {
            if (produced.Owner == SelfPlayer && produced.Info.Name != "harv" && !EsuAIUtils.IsActorOfType(World, produced, EsuAIConstants.ProductionCategories.BUILDING)
                && !EsuAIUtils.IsActorOfType(World, produced, EsuAIConstants.ProductionCategories.DEFENSE))
            {
                OffensiveActorsCache.Add(produced);
            }
        }

        public void AddUnitOrdersIfApplicable(StrategicWorldState state, Queue<Order> orders)
        {
            RemoveDeadActorsFromCache();

            UnitProductionCheckCooldown --;
            if (UnitProductionCheckCooldown > 0) {
                return;
            }

            ChooseAndProduceUnit(state, orders);
        }

        private void RemoveDeadActorsFromCache()
        {
            for (int i = OffensiveActorsCache.Count - 1; i >= 0; i--)
            {
                if (OffensiveActorsCache[i].IsDead)
                {
                    OffensiveActorsCache.RemoveAt(i);
                }
            }
        }

        private void ChooseAndProduceUnit(StrategicWorldState state, Queue<Order> orders)
        {
            // Give harvesters precedence.
            if (ProduceHarvesterIfApplicable(state, orders)) {
                return;
            }

            // TODO : debug code (we want to base this off of the aggregate info).
            ProduceUnitForDistribution(state, orders, DefaultInfantryPercentage, DefaultVehiclePercentage, 0d);
        }

        /** @return true if a harvester production order was issued this call, false otherwise. */
        private bool ProduceHarvesterIfApplicable(StrategicWorldState state, Queue<Order> orders)
        {
            if (EsuAIUtils.IsItemCurrentlyInProductionForCategory(state.World, SelfPlayer, EsuAIConstants.ProductionCategories.VEHICLE, EsuAIConstants.Vehicles.HARVESTER) ||
                    EsuAIUtils.IsItemCurrentlyInProductionForCategory(state.World, SelfPlayer, EsuAIConstants.ProductionCategories.BUILDING, EsuAIConstants.Buildings.WAR_FACTORY))
            {
                return false;
            }

            if (NumOreRefineriesBuilt(state) < Info.MinNumRefineries)
            {
                return false;
            }

            if (NumLivingHarvesters(state) < Info.MinNumHarvesters 
                && (!EsuAIUtils.IsAnyItemCurrentlyInProductionForCategory(World, SelfPlayer, EsuAIConstants.ProductionCategories.VEHICLE)))
            {
                ProduceVehicle(state, orders, EsuAIConstants.Vehicles.HARVESTER);
                return true;
            }

            return false;
        }

        private int NumOreRefineriesBuilt(StrategicWorldState state)
        {
            return state.World.Actors.Count(a => a.Owner == SelfPlayer && a.IsInWorld
                && !a.IsDead && a.TraitOrDefault<Refinery>() != null);
        }

        private int NumLivingHarvesters(StrategicWorldState state)
        {
            return state.World.ActorsHavingTrait<Harvester>().Count(a => a.Owner == SelfPlayer && !a.IsDead);
        }

        // Produces units for the specified distribution. If we currently have excess resources, we will produce units without taking the distribution into account.
        private void ProduceUnitForDistribution(StrategicWorldState state, Queue<Order> orders, double infantryPercent, double vehiclePercent, double airPercent)
        {
            double currentResources = EsuAIUtils.GetCurrentResourcesForPlayer(SelfPlayer);

            if (!EsuAIUtils.IsAnyItemCurrentlyInProductionForCategory(World, SelfPlayer, EsuAIConstants.ProductionCategories.INFANTRY))
            {
                // TODO: Check here for vehicle and air.
                double currentInfantryPercentage = PercentageOfSelfOffensiveUnitsCurrentlyInWorldOfType(EsuAIConstants.ProductionCategories.INFANTRY);
                if (currentInfantryPercentage < infantryPercent || currentResources > Info.ExcessResourceLevel)
                {
                    ProduceInfantry(state, orders);
                }
            }

            if (!EsuAIUtils.IsAnyItemCurrentlyInProductionForCategory(World, SelfPlayer, EsuAIConstants.ProductionCategories.VEHICLE))
            { 
                double currentVehiclePercentage = PercentageOfSelfOffensiveUnitsCurrentlyInWorldOfType(EsuAIConstants.ProductionCategories.VEHICLE);
                if (currentVehiclePercentage < vehiclePercent || currentResources > Info.ExcessResourceLevel)
                {
                    var vehicleName = GetVehicleToProduce(state);
                    ProduceVehicle(state, orders, vehicleName);
                }
            }
        }

        private double PercentageOfSelfOffensiveUnitsCurrentlyInWorldOfType(string type)
        {
            if (OffensiveActorsCache.Count() == 0) {
                return 0;
            }

            // Don't include harvesters (not offense)
            var actors = OffensiveActorsCache.Where(a => EsuAIUtils.IsActorOfType(World, a, type) && a.Info.Name != "harv");
            return ((double) actors.Count() / (double) OffensiveActorsCache.Count());
        }

        private void ProduceInfantry(StrategicWorldState state, Queue<Order> orders)
        {
            var infantry = GetInfantryToProduce(state);
            bool wasOrderIssued = ProduceUnit(state, orders, infantry, EsuAIConstants.ProductionCategories.INFANTRY);
            if (!wasOrderIssued) {
                ScheduleBuildingProduction(EsuAIConstants.Buildings.GetBarracksNameForPlayer(SelfPlayer), state, orders);
            }
            UnitProductionCheckCooldown = UNIT_PRODUCTION_COOLDOWN;
        }

        private string GetInfantryToProduce(StrategicWorldState state)
        {
            Dictionary<string, DamageKillStats> infantryStats = state.UnitStatsLoader.GetStatsForActors(EsuAIConstants.Infantry.AVAILABLE_WITH_BARRACKS);
            if (infantryStats == null || ShouldProduceRandomUnit()) {
                return EsuAIConstants.Infantry.AVAILABLE_WITH_BARRACKS.Random(RANDOM);
            } else {
                return state.UnitStatsLoader.GetUnitForStats(infantryStats);
            }
        }

        private bool ShouldProduceRandomUnit()
        {
            float chooseRandom = RANDOM.NextFloat();
            return chooseRandom <= Info.GetUnitProductionRandomPercentage();
        }

        private void ProduceVehicle(StrategicWorldState state, Queue<Order> orders, string vehicleName)
        {
            bool wasOrderIssued = ProduceUnit(state, orders, vehicleName, EsuAIConstants.ProductionCategories.VEHICLE);
            if (!wasOrderIssued) {
                ScheduleBuildingProduction(EsuAIConstants.Buildings.WAR_FACTORY, state, orders);
            }
            UnitProductionCheckCooldown = UNIT_PRODUCTION_COOLDOWN;
        }

        private string GetVehicleToProduce(StrategicWorldState state)
        {
            Dictionary<string, DamageKillStats> vehicleStats = state.UnitStatsLoader.GetStatsForActors(EsuAIConstants.Vehicles.GetVehiclesForPlayer(SelfPlayer));
            if (vehicleStats == null || ShouldProduceRandomUnit()) {
                return EsuAIConstants.Vehicles.GetRandomVehicleForPlayer(SelfPlayer);
            } else {
                return state.UnitStatsLoader.GetUnitForStats(vehicleStats);
            }
        }

        private bool ProduceUnit(StrategicWorldState state, Queue<Order> orders, string unitName, string productionCategory)
        {
            var queues = EsuAIUtils.FindProductionQueuesForPlayerAndCategory(World, SelfPlayer, productionCategory);

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
            orders.Enqueue(Order.StartProduction(SelfPlayer.PlayerActor, unitName, 1));
            return false;
        }

        /** @return - true if the building production has been scheduled, false otherwise. */
        private bool ScheduleBuildingProduction(string building, StrategicWorldState state, Queue<Order> orders)
        {
            if (!state.RequestedBuildingQueue.Contains(building)
                    && !EsuAIUtils.DoesItemCurrentlyExistOrIsBeingProducedForPlayer(World, SelfPlayer, building)) 
            {
                state.RequestedBuildingQueue.Enqueue(building);
                UnitProductionCheckCooldown = UNIT_PRODUCTION_COOLDOWN;
                return true;
            }
            return false;
        }
    }
}
