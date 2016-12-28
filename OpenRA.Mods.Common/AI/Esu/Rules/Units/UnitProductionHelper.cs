﻿using System;
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
        private int unitProductionCooldown;

        private readonly World world;
        private readonly Player selfPlayer;
        private readonly EsuAIInfo info;

        public UnitProductionHelper(World world, Player selfPlayer, EsuAIInfo info)
        {
            this.world = world;
            this.selfPlayer = selfPlayer;
            this.info = info;
        }

        public void OnOrderDenied(Order order)
        {
            /* Do Nothing. */
        }

        public void AddUnitOrdersIfApplicable(Actor self, StrategicWorldState state, Queue<Order> orders)
        {
            unitProductionCooldown --;
            if (unitProductionCooldown > 0) {
                return;
            }

            ChooseAndProduceUnit(self, state, orders);
        }

        private void ChooseAndProduceUnit(Actor self, StrategicWorldState state, Queue<Order> orders)
        {
            // TODO We don't necessarily want the best fit cell.
            AggregateScoutReportData data = state.ScoutReportGrid.GetCurrentBestFitCell();
            if (data == null) {
                ProduceUnitForDistribution(self, state, orders, DefaultInfantryPercentage, DefaultVehiclePercentage, 0d);
            }

            // TODO : debug code (we want to base this off of the aggregate info).
            ProduceUnitForDistribution(self, state, orders, DefaultInfantryPercentage, DefaultVehiclePercentage, 0d);
        }

        private void ProduceUnitForDistribution(Actor self, StrategicWorldState state, Queue<Order> orders, double infantryPercent, double vehiclePercent, double airPercent)
        {
            // TODO: Check here for vehicle and air.
            double currentInfantryPercentage = PercentageOfSelfOffensiveUnitsCurrentlyInWorldOfType(EsuAIConstants.ProductionCategories.INFANTRY);
            if (currentInfantryPercentage < infantryPercent)
            {
                ProduceInfantry(self, state, orders);
            }
            else
            {
                ProduceVehicle(self, state, orders);
            }
        }

        private double PercentageOfSelfOffensiveUnitsCurrentlyInWorldOfType(string type)
        {
            var actors = world.Actors.Where(a => a.Owner == selfPlayer && !a.IsDead && EsuAIUtils.IsActorOfType(world, a, type));
            // TODO yech... find a better way to get this.
            // All offensive actors.
            var allActors = world.Actors.Where(a => a.Owner == selfPlayer && !a.IsDead && a.Info.Name != "harv" && !EsuAIUtils.IsActorOfType(world, a, EsuAIConstants.ProductionCategories.BUILDING)
                && !EsuAIUtils.IsActorOfType(world, a, EsuAIConstants.ProductionCategories.DEFENSE));

            if (allActors.Count() == 0) {
                return 0;
            }
            return ((double) actors.Count() / (double) allActors.Count());
        }

        private void ProduceInfantry(Actor self, StrategicWorldState state, Queue<Order> orders)
        {
            if (EsuAIUtils.IsAnyItemCurrentlyInProductionForCategory(world, selfPlayer, EsuAIConstants.ProductionCategories.INFANTRY)) {
                return;
            }

            var infantry = GetInfantryToProduce();

            try {
                ProduceUnit(self, state, orders, infantry, EsuAIConstants.ProductionCategories.INFANTRY);
            } catch (UnbuildableException) {
                // We have no production queue for infantry yet, which means we need a barracks.
                ScheduleBuildingProduction(EsuAIConstants.Buildings.GetBarracksNameForPlayer(selfPlayer), state, orders);
            }
        }

        // TODO: This is mostly for debug purposes, we don't want to just build random infantry.
        private string GetInfantryToProduce()
        {
            return EsuAIConstants.Infantry.AVAILABLE_WITH_BARRACKS.Random(RANDOM);
        }

        private void ProduceVehicle(Actor self, StrategicWorldState state, Queue<Order> orders)
        {
            if (EsuAIUtils.IsAnyItemCurrentlyInProductionForCategory(world, selfPlayer, EsuAIConstants.ProductionCategories.INFANTRY)) {
                return;
            }

            var vehicle = GetVehicleToProduce();

            try {
                ProduceUnit(self, state, orders, vehicle, EsuAIConstants.ProductionCategories.VEHICLE);
            } catch (UnbuildableException) {
                // We have no production queue for vehicles yet, which means we need a war factory.
                ScheduleBuildingProduction(EsuAIConstants.Buildings.WAR_FACTORY, state, orders);
            }
        }

        // TODO: This is mostly for debug purposes, we don't want to just build random vehicles.
        private string GetVehicleToProduce()
        {
            return EsuAIConstants.Vehicles.GetRandomVehicleForPlayer(selfPlayer);
        }

        private void ProduceUnit(Actor self, StrategicWorldState state, Queue<Order> orders, string unitName, string productionCategory)
        {
            var queues = EsuAIUtils.FindProductionQueuesForPlayerAndCategory(world, selfPlayer, productionCategory);

            var buildable = queues.First().AllItems().FirstOrDefault(a => a.Name == unitName);
            if (buildable == null) {
                throw new UnbuildableException("No queues to build " + unitName);
            }

            var prereqs = buildable.TraitInfo<BuildableInfo>().Prerequisites.Where(s => !s.StartsWith("~"));
            foreach (string req in prereqs) {
                if (ScheduleBuildingProduction(req, state, orders)) {
                    // We have scheduled construction of this building.
                    return;
                }
            }

            // We can build now.
            orders.Enqueue(Order.StartProduction(self, unitName, 1));
            unitProductionCooldown = UNIT_PRODUCTION_COOLDOWN;
        }

        /** @return - true if the building production has been scheduled, false otherwise. */
        private bool ScheduleBuildingProduction(string building, StrategicWorldState state, Queue<Order> orders)
        {
            if (!state.RequestedBuildingQueue.Contains(building)
                    && !EsuAIUtils.DoesItemCurrentlyExistOrIsBeingProducedForPlayer(world, selfPlayer, building)) 
            {
                state.RequestedBuildingQueue.Enqueue(building);
                unitProductionCooldown = UNIT_PRODUCTION_COOLDOWN;
                return true;
            }
            return false;
        }
    }

    public class UnbuildableException : SystemException
    {
        public UnbuildableException(String message) : base(message)
        {
        }
    }
}