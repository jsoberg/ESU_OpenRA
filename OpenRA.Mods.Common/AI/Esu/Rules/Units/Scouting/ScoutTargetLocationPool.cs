using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRA.Mods.Common.AI.Esu.Strategy;

namespace OpenRA.Mods.Common.AI.Esu.Rules.Units
{
    public class ScoutTargetLocationPool
    {
        private readonly Queue<CPos> AvailablePositions;

        public ScoutTargetLocationPool()
        {
            this.AvailablePositions = new Queue<CPos>();
        }

        public CPos GetTargetLocationForScout(World world, StrategicWorldState state)
        {
            // TODO
            return CPos.Invalid;
        }
    }
}
