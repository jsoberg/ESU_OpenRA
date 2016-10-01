using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRA.Mods.Common.AI.Esu.Strategy;

namespace OpenRA.Mods.Common.AI.Esu.Rules
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

        public virtual void Activate(Player selfPlayer)
        {
            this.selfPlayer = selfPlayer;
        }

        public virtual void Tick(Actor self, StrategicWorldState state, Queue<Order> orders)
        {
            AddOrdersForTick(self, state, orders);
        }

        public abstract void AddOrdersForTick(Actor self, StrategicWorldState state, Queue<Order> orders);
    }
}
