using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRA.Mods.Common.AI.Esu.Rules
{
    public class UnitHelper
    {
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
            // Stub.
        }
    }
}
