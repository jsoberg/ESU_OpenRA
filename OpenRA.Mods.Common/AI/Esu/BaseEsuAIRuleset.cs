using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRA.Mods.Common.AI.Esu
{
    [Desc("Base class for the EsuAI to use to get and execute rules.")]
    public abstract class BaseEsuAIRuleset
    {
        protected readonly World world;
        protected readonly EsuAIInfo info;

        protected Player selfPlayer;

        public BaseEsuAIRuleset(World world, EsuAIInfo info)
        {
            this.world = world;
            this.info = info;
        }

        public void Activate(Player selfPlayer)
        {
            this.selfPlayer = selfPlayer;
        }

        public IEnumerable<Order> Tick(Actor self)
        {
            Queue<Order> orders = new Queue<Order>();
            AddOrdersForTick(self, orders);
            return orders;
        }

        public abstract void AddOrdersForTick(Actor self, Queue<Order> orders);
    }
}
