using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRA.Mods.Common.AI.Esu.Rules.Units.Defense
{
    public class DefenseHelper
    {
        private readonly World World;
        private readonly Player SelfPlayer;
        private readonly EsuAIInfo Info;

        public DefenseHelper(World world, Player selfPlayer, EsuAIInfo info)
        {
            this.World = world;
            this.SelfPlayer = selfPlayer;
            this.Info = info;
        }
    }
}
