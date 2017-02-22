using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRA.Mods.Common.AI.Esu.Strategy;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.AI.Esu.Rules.Resources
{
    public class ResourceGatheringRuleset : BaseEsuAIRuleset
    {
        private const int ResourcesEarnedTickInterval = 2000;
        private const int TickCheckCount = 100;

        private int LastItemProducedTick;
        private int ResourcesEarnedBeginningOfLastTickInterval;

        public ResourceGatheringRuleset(World world, EsuAIInfo info) : base(world, info)
        {
        }

        public override void AddOrdersForTick(Actor self, StrategicWorldState state, Queue<Order> orders)
        {
            var ticks = state.World.GetCurrentLocalTickCount();
            if (ticks % ResourcesEarnedTickInterval == 0) {
                // If the min number of refineries and harvesters aren't built yet, let the other rulesets handle this.
                if (MinNumOreRefineriesBuilt() && MinNumHarvestersProduced())
                {
                    ProduceNewResourceGatheringItemIfApplicable(self, state, orders);
                }

                UpdateResourcesEarnedBeginningOfLastInterval();
            }
        }

        private void UpdateResourcesEarnedBeginningOfLastInterval()
        {
            ResourcesEarnedBeginningOfLastTickInterval = selfPlayer.PlayerActor.Trait<PlayerResources>().Earned;
        }

        private bool MinNumOreRefineriesBuilt()
        {
            var ownedActors = world.ActorsHavingTrait<Refinery>().Count(a => a.Owner == selfPlayer && a.IsInWorld 
                && !a.IsDead);
            return (ownedActors >= info.MinNumRefineries);
        }

        private bool MinNumHarvestersProduced()
        {
            int harvesters = world.ActorsHavingTrait<Harvester>().Count(a => a.Owner == selfPlayer && !a.IsDead);
            return (harvesters >= info.MinNumHarvesters);
        }

        private void ProduceNewResourceGatheringItemIfApplicable(Actor self, StrategicWorldState state, Queue<Order> orders)
        {
            if ((selfPlayer.PlayerActor.Trait<PlayerResources>().Earned - ResourcesEarnedBeginningOfLastTickInterval) > info.EarnedResourcesThreshold)
            {
                return;
            }

            if (!EsuAIUtils.IsAnyItemCurrentlyInProductionForCategory(state.World, selfPlayer, EsuAIConstants.ProductionCategories.VEHICLE))
            {
                Order order = Order.StartProduction(self, EsuAIConstants.Vehicles.HARVESTER, 1);
                orders.Enqueue(order);
                LastItemProducedTick = state.World.GetCurrentLocalTickCount();
            }
            else
            {
                state.RequestedBuildingQueue.Enqueue(EsuAIConstants.Buildings.ORE_REFINERY);
                LastItemProducedTick = state.World.GetCurrentLocalTickCount();
            }
        }
    }
}
