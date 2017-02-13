using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRA.Mods.Common.AI.Esu.Strategy;

namespace OpenRA.Mods.Common.AI.Esu.Rules.Resources
{
    public class ResourceGatheringRuleset : BaseEsuAIRuleset
    {
        public ResourceGatheringRuleset(World world, EsuAIInfo info) : base(world, info)
        {
        }

        public override void AddOrdersForTick(Actor self, StrategicWorldState state, Queue<Order> orders)
        {
            
        }
    }
}
