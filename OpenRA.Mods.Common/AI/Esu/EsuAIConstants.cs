using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRA.Support;

namespace OpenRA.Mods.Common.AI.Esu
{
    public static class EsuAIConstants
    {
        private static MersenneTwister RANDOM = new MersenneTwister(DateTime.Now.Millisecond);

        private const string ALLIES = "Allies";
        private const string SOVIET = "Soviet";

        public static class Buildings
        {
            public const string CONSTRUCTION_YARD = "fact";
            public const string POWER_PLANT = "powr";
            public const string ADVANCED_POWER_PLANT = "apowr";
            public const string ORE_REFINERY = "proc";

            public static class Allies
            {
                public static string BARRACKS = "tent";
            }

            public static class Soviet
            {
                public static string BARRACKS = "barr";
            }

            public static string GetBarracksNameForPlayer(Player player)
            {
                switch (player.Faction.Side)
                {
                    case ALLIES:
                        return Allies.BARRACKS;
                    case SOVIET:
                        return Soviet.BARRACKS;
                    default:
                        throw new SystemException("Unknown faction side: " + player.Faction.Side);
                }
            }
        }

        public static class Defense
        {
            public static class Allies
            {
                public static string TURRET = "gun";
                public static string ANTI_AIR_GUN = "agun";
                public static string PILL_BOX = "pbox";
                public static string CAMO_PILL_BOX = "hbox";

                public static string[] VALUES = {
                    TURRET,
                    ANTI_AIR_GUN,
                    PILL_BOX,
                    CAMO_PILL_BOX
                };
            }

            public static class Soviet
            {
                public static string SAM_SITE = "sam";
                public static string FLAME_TOWER = "ftur";
                public static string TESLA = "tsla";

                public static string[] VALUES = {
                    SAM_SITE,
                    FLAME_TOWER,
                    TESLA
                };
            }

            public static string GetRandomDefenseStructureForPlayer(Player player)
            {
                switch (player.Faction.Side)
                {
                    case ALLIES:
                        return Allies.VALUES.Random(RANDOM);
                    case SOVIET:
                        return Soviet.VALUES.Random(RANDOM);
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
            public const string DEFENSE = "Defense";
        }
    }
}
