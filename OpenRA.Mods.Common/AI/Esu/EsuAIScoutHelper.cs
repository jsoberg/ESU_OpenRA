using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.Common.AI.Esu
{
    class EsuAIScoutHelper
    {
        private readonly World world;
        private readonly Player selfPlayer;
        private readonly EsuAIInfo info;

        private readonly List<Actor> currentScouts;
        private string scoutInProductionName;

        public EsuAIScoutHelper(World world, Player selfPlayer, EsuAIInfo info)
        {
            this.world = world;
            this.selfPlayer = selfPlayer;
            this.info = info;

            this.currentScouts = new List<Actor>();
        }

        public void AddBuildNewScoutOrderIfApplicable(Actor self, Queue<Order> orders) 
        {
            if (!ShouldBuildNewScout()) {
                return;
            }

            AddBuildNewScoutOrder(self, orders);
        }

        private bool ShouldBuildNewScout()
        {
            // TODO: We might want more than one scout, so the second half of this check might not make sense.
            if (scoutInProductionName != null || currentScouts.Count > 0) {
                return false;
            }

            var productionQueues = EsuAIUtils.FindProductionQueues(world, selfPlayer, EsuAIConstants.ProductionCategories.INFANTRY);
            if (productionQueues.Count() == 0) {
                // We aren't able to build a scout right now.
                return false;
            }

            return true;
        }

        private void AddBuildNewScoutOrder(Actor self, Queue<Order> orders)
        {
            scoutInProductionName = GetBestAvailableScoutName();
            if (scoutInProductionName == null) {
                return;
            }

            orders.Enqueue(Order.StartProduction(self, scoutInProductionName, 1));
        }

        [Desc("Uses the current world state to find the best available scouting unit to build.")]
        private string GetBestAvailableScoutName()
        {
            var productionQueues = EsuAIUtils.FindProductionQueues(world, selfPlayer, EsuAIConstants.ProductionCategories.INFANTRY);
            foreach (ProductionQueue queue in productionQueues) {
                // TODO faction checks for dogs?
                if (queue.BuildableItems().Count(a => a.Name == EsuAIConstants.Infantry.RIFLE_INFANTRY) > 0) {
                    return EsuAIConstants.Infantry.RIFLE_INFANTRY;
                }
            }

            return null;
        }
    }
}
