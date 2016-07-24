using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRA.Mods.Common.AI.Esu
{
    public static class EsuAIConstants
    {
        private const string ALLIES = "Allies";
        private const string SOVIET = "Soviet";

        public static class Buildings
        {
            public const string CONSTRUCTION_YARD = "fact";
            public const string POWER_PLANT = "powr";
            public const string ORE_REFINERY = "proc";

            private static string ALLIED_BARRACKS = "tent";
            private static string SOVIET_BARRACKS = "barr";

            public static string GetBarracksNameForPlayer(Player player)
            {
                switch (player.Faction.Side) {
                    case ALLIES:
                        return ALLIED_BARRACKS;
                    case SOVIET:
                        return SOVIET_BARRACKS;
                    default:
                        throw new SystemException("Unknown faction side: " + player.Faction.Side);
                }
            }
        }

        public static class Infantry
        {
            public const string RIFLE_INFANTRY = "e1";
            public const string GRENADIER = "e2";
            public const string ROCKET_SOLDIER = "e3";
            public const string FLAMETHROWER = "e4";
            public const string ENGINEER = "e6";
        }

        public static class ProductionCategories
        {
            public const string BUILDING = "Building";
            public const string INFANTRY = "Infantry";
        }
    }
}
