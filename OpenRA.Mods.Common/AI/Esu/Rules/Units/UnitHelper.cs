using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.Common.AI.Esu.Rules
{
    public class UnitHelper
    {
        private const int UNIT_PRODUCTION_COOLDOWN = 5;
        private int unitProductionCooldown;

        private readonly World world;
        private readonly Player selfPlayer;
        private readonly EsuAIInfo info;

        public UnitHelper(World world, Player selfPlayer, EsuAIInfo info)
        {
            this.world = world;
            this.selfPlayer = selfPlayer;
            this.info = info;
        }

        // @return - returns true if the produced unit was claimed, false otherwise.
        public bool UnitProduced(Actor self, Actor other)
        {
            // Stub.
            return false;
        }

        public void AddUnitOrdersIfApplicable(Actor self, StrategicWorldState state, Queue<Order> orders)
        {
            unitProductionCooldown --;
            if (unitProductionCooldown > 0) {
                return;
            }

            ProduceVehicle(self, state, orders);
        }


        private void ProduceVehicle(Actor self, StrategicWorldState state, Queue<Order> orders)
        {
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
