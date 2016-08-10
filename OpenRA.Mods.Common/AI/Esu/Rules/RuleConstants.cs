using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRA.Mods.Common.AI.Esu.Rules
{
    public static class RuleConstants
    {
        public static class DefensiveBuildingPlacementValues
        {
            public const int CLOSEST_TO_CONSTRUCTION_YARD = 0;
            public const int RANDOM = 1;
            public const int DISTRIBUTED_TO_IMPORTANT_STRUCTURES = 2;
        }
    }
}
