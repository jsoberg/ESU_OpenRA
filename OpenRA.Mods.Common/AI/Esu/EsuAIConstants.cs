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

        public static class OrderTypes
        {
            public const string PRODUCTION_ORDER = "StartProduction";
        }

        private const string ALLIES = "Allies";
        private const string SOVIET = "Soviet";

        public static class Buildings
        {
            public const string CONSTRUCTION_YARD = "fact";
            public const string POWER_PLANT = "powr";
            public const string ADVANCED_POWER_PLANT = "apowr";
            public const string ORE_REFINERY = "proc";
            public const string WAR_FACTORY = "weap";

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

                public static string[] VALUES_NO_AA = {
                    TURRET,
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


                public static string[] VALUES_NO_AA = {
                    FLAME_TOWER,
                    TESLA
                };
            }

            public static string[] VALUES = {
                Allies.TURRET,
                Allies.ANTI_AIR_GUN,
                Allies.PILL_BOX,
                Allies.CAMO_PILL_BOX,
                Soviet.SAM_SITE,
                Soviet.FLAME_TOWER,
                Soviet.TESLA
            };

            public static bool IsAntiInfantry(string buildingName)
            {
                return (buildingName == Allies.PILL_BOX ||
                    buildingName == Allies.CAMO_PILL_BOX ||
                    buildingName == Soviet.FLAME_TOWER ||
                    buildingName == Soviet.TESLA);
            }

            public static bool IsAntiVehicle(string buildingName)
            {
                // TODO: Description for TESLA states that it is used for anti-infantry OR anti-vehicle. Combined category, or leave with infantry?
                return (buildingName == Allies.TURRET);
            }

            public static bool IsAntiAir(string buildingName)
            {
                return (buildingName == Allies.ANTI_AIR_GUN ||
                     buildingName == Soviet.SAM_SITE);
            }

            public static string GetRandomDefenseStructureForPlayer(Player player)
            {
                return GetDefenseStructuresForPlayer(player).Random(RANDOM);
            }

            public static string[] GetDefenseStructuresForPlayer(Player player)
            {
                switch (player.Faction.Side)
                {
                    case ALLIES:
                        return Allies.VALUES_NO_AA;
                    case SOVIET:
                        return Soviet.VALUES_NO_AA;
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

            public static string[] AVAILABLE_WITH_BARRACKS = {
                RIFLE_INFANTRY,
                GRENADIER, 
                ROCKET_SOLDIER
            };

            public static string[] VALUES = {
                RIFLE_INFANTRY,
                GRENADIER,
                ROCKET_SOLDIER,
                FLAMETHROWER,
                ENGINEER
            };
        }

        public static class Vehicles
        {
            public const string HARVESTER = "harv";

            public static class Soviet
            {
                public const string V2_ROCKET = "v2rl";
                public const string MAMMOTH_TANK = "4tnk";
                public const string APC = "apc";
                public const string MOBILE_FLAK = "ftrk";
                //public const string MAD_TANK = "qtnk";

                public static string[] VALUES = {
                    V2_ROCKET,
                    MAMMOTH_TANK,
                    APC,
                    MOBILE_FLAK,
                    //MAD_TANK
                };
            }

            public static class Allies
            {
                public const string LIGHT_TANK = "1tnk";
                public const string MEDIUM_TANK = "2tnk";
                public const string HEAVY_TANK = "3tnk";
                public const string ARTILLERY = "arty";
                public const string RANGER = "jeep";

                public static string[] VALUES = {
                    LIGHT_TANK,
                    MEDIUM_TANK,
                    HEAVY_TANK,
                    ARTILLERY,
                    RANGER
                };
            }

            public static string[] VALUES = {
                Soviet.V2_ROCKET,
                Soviet.MAMMOTH_TANK,
                Soviet.APC,
                Soviet.MOBILE_FLAK,
                //Soviet.MAD_TANK,
                Allies.LIGHT_TANK,
                Allies.MEDIUM_TANK,
                Allies.HEAVY_TANK,
                Allies.ARTILLERY,
                Allies.RANGER
            };

            public static string GetRandomVehicleForPlayer(Player player)
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

            public static string[] GetVehiclesForPlayer(Player player)
            {
                switch (player.Faction.Side)
                {
                    case ALLIES:
                        return Allies.VALUES;
                    case SOVIET:
                        return Soviet.VALUES;
                    default:
                        throw new SystemException("Unknown faction side: " + player.Faction.Side);
                }
            }
        }

        public static class ProductionCategories
        {
            public const string BUILDING = "Building";
            public const string INFANTRY = "Infantry";
            public const string DEFENSE = "Defense";
            public const string VEHICLE = "Vehicle";
            public const string AIRCRAFT = "Plane";
        }
    }
}
