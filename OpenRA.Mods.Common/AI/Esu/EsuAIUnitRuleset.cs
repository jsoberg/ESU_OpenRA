using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.Common.AI.Esu
{
    class EsuAIUnitRuleset : BaseEsuAIRuleset
    {
        private EsuAIScoutHelper scoutHelper;

        public EsuAIUnitRuleset(World world, EsuAIInfo info) : base(world, info)
        {
        }

        public override void Activate(Player selfPlayer)
        {
            base.Activate(selfPlayer);
            this.scoutHelper = new EsuAIScoutHelper(world, selfPlayer, info);
        }

        public override void AddOrdersForTick(Actor self, Queue<Order> orders)
        {
            scoutHelper.AddBuildNewScoutOrderIfApplicable(self, orders);
        }

    }
}
