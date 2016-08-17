using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRA.Mods.Common.AI.Esu;

namespace OpenRA.Mods.Common.AI.Esu.Rules
{
    public static class RuleConstants
    {
        public static class DefensiveBuildingPlacementValues
        {
            public const int CLOSEST_TO_CONSTRUCTION_YARD = 0;
            public const int DISTRIBUTED_TO_IMPORTANT_STRUCTURES = 1;
            public const int RANDOM = 2;

            public static readonly string[] IMPORTANT_STRUCTURES = {
                EsuAIConstants.Buildings.CONSTRUCTION_YARD,
                EsuAIConstants.Buildings.ADVANCED_POWER_PLANT,
                EsuAIConstants.Buildings.POWER_PLANT,
                EsuAIConstants.Buildings.ORE_REFINERY
            };
        }

        public static class NormalBuildingPlacementValues
        {
            public const int FARTHEST_FROM_ENEMY_LOCATIONS = 0;
            public const int RANDOM = 1;
        }
    }
}
