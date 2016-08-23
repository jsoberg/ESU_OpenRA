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

            ProduceRandomVehicle(self, state, orders);
        }

        // TODO: This is mostly for debug purposes, we don't want to just build random vehicles.
        private void ProduceRandomVehicle(Actor self, StrategicWorldState state, Queue<Order> orders)
        {
            var vehicle = EsuAIConstants.Vehicles.GetRandomVehicleForPlayer(selfPlayer);

            var queues = EsuAIUtils.FindProductionQueuesForPlayerAndCategory(world, selfPlayer, EsuAIConstants.ProductionCategories.VEHICLE);
            // TODO not first, decide where to go.
            var buildable = queues.First().AllItems().FirstOrDefault(a => a.Name == vehicle);
            if (buildable == null) {
                ScheduleBuildingProduction(EsuAIConstants.Buildings.WAR_FACTORY, state, orders);
                return;
            }

            var prereqs = buildable.TraitInfo<BuildableInfo>().Prerequisites.Where(s => !s.StartsWith("~"));
            foreach (string req in prereqs) {
                if (ScheduleBuildingProduction(req, state, orders)) {
                    return;
                }
            }

            // We can build now.
            orders.Enqueue(Order.StartProduction(self, vehicle, 1));
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
}
