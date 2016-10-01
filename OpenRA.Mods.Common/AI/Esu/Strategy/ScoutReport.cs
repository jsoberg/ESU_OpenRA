using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRA.Mods.Common.AI.Esu;
using OpenRA.Mods.Common.AI.Esu.Geometry;

namespace OpenRA.Mods.Common.AI.Esu.Strategy
{
    public class ScoutReport
    {
        /// <summary>
        ///  The recommended unit response and urgency for this report.
        /// </summary>
        public readonly ResponseRecommendation ResponseRecommendation;

        private readonly Dictionary<Actor, int> ActorToRecommendationMap;
        private long LastRefreshTick;

        public ScoutReport()
        {

        }

        public long GetLastRefreshTick()
        {
            return LastRefreshTick;
        }
    }

    public static class RecommendationAlgorithm
    {
        public const int PowerPlants = 0;
        public const int DefensiveBuildings = 1;
        public const int Units = 2;
        public const int OreRefineries = 3;
        public const int Combinatorial = 4;
    };

    public static class UrgencyAlgorithm
    {
        public const int UnitsAndDefensiveStructures = 0;
        public const int BuildingsPerUnitAndDefensiveStructure = 1;
        public const int SameAsResponseRecommendation = 2;
    };

    public class ResponseRecommendation
    {
        private const float COMBINATORIAL_FACTOR = .25f;

        public readonly int UnitResponseValue;
        public readonly int UrgencyValue;

        public ResponseRecommendation(Builder builder)
        {
            this.UnitResponseValue = ComputeUnitResponseValue(builder);
            this.UrgencyValue = ComputeUrgency(builder, UnitResponseValue);
        }

        private int ComputeUnitResponseValue(Builder builder)
        {
            switch (builder.info.ScoutRecommendationEnumAlgorithm) {
                case RecommendationAlgorithm.PowerPlants:
                    return PowerPlants(builder);
                case RecommendationAlgorithm.DefensiveBuildings:
                    return DefensiveBuildings(builder);
                case RecommendationAlgorithm.Units:
                    return Units(builder);
                case RecommendationAlgorithm.OreRefineries:
                    return OreRefineries(builder);
                case RecommendationAlgorithm.Combinatorial:
                    return Combinatorial(builder);
                default:
                    throw new SystemException("Unknown recommendation algorithm " + builder.info.ScoutRecommendationEnumAlgorithm);
            };
        }

        // ========================================
        // Response Algorithms
        // ========================================

        private int PowerPlants(Builder builder)
        {
            return (builder.numPowerPlants + (2 * builder.numPowerPlants)) * builder.info.ScoutRecommendationImportanceMultiplier;
        }

        private int DefensiveBuildings(Builder builder)
        {
            return builder.numDefensiveBuildings * builder.info.ScoutRecommendationImportanceMultiplier;
        }

        private int Units(Builder builder)
        {
            return builder.AllUnits() * builder.info.ScoutRecommendationImportanceMultiplier;
        }

        private int OreRefineries(Builder builder)
        {
            return builder.numOreRefineries * builder.info.ScoutRecommendationImportanceMultiplier;
        }

        private int Combinatorial(Builder builder)
        {
            return (int) ((PowerPlants(builder) * COMBINATORIAL_FACTOR) + (DefensiveBuildings(builder) * COMBINATORIAL_FACTOR) 
                + (Units(builder) * COMBINATORIAL_FACTOR) + (OreRefineries(builder) * COMBINATORIAL_FACTOR) * builder.info.ScoutRecommendationImportanceMultiplier);
        }

        // ========================================
        // Urgency Algorithms
        // ========================================

        private int ComputeUrgency(Builder builder, int responseRecommendation)
        {
            switch (builder.info.ScoutReportUrgencyAlgorithm) {
                case UrgencyAlgorithm.UnitsAndDefensiveStructures:
                    return UnitsAndDefensiveStructures(builder);
                case UrgencyAlgorithm.BuildingsPerUnitAndDefensiveStructure:
                    return BuildingsPerUnitAndDefensiveStructure(builder);
                case UrgencyAlgorithm.SameAsResponseRecommendation:
                    return responseRecommendation;
                default:
                    throw new SystemException("Unknown urgency algorithm " + builder.info.ScoutReportUrgencyAlgorithm);
            }
        }

        private int UnitsAndDefensiveStructures(Builder builder)
        {
            return (builder.AllUnits() + builder.numDefensiveBuildings);
        }

        private int BuildingsPerUnitAndDefensiveStructure(Builder builder)
        {
            int unitsAndDefensiveStructures = UnitsAndDefensiveStructures(builder);
            unitsAndDefensiveStructures = (unitsAndDefensiveStructures >= 1) ? unitsAndDefensiveStructures : 1;

            int buildings = ((builder.numPowerPlants + builder.numAdvancedPowerPlants) + builder.numOreRefineries + builder.numOtherBuildings);
            return buildings / unitsAndDefensiveStructures;
        }

        public class Builder
        {
            internal readonly EsuAIInfo info;

            internal int numPowerPlants;
            internal int numAdvancedPowerPlants;

            internal int numDefensiveBuildings;

            internal int numInfantryUnits;
            internal int numVehicleUnits;
            internal int numAircraftUnits;

            internal int numOreRefineries;

            internal int numOtherBuildings;

            public Builder(EsuAIInfo info)
            {
                this.info = info;
            }

            public int AllUnits()
            {
                return (numAircraftUnits + numInfantryUnits + numVehicleUnits);
            }

            public Builder AddPowerPlant()
            {
                this.numPowerPlants++;
                return this;
            }

            public Builder AddAdvancedPowerPlant()
            {
                this.numAdvancedPowerPlants++;
                return this;
            }

            public Builder SetNumPowerPlants(int numPowerPlants, int numAdvancedPowerPlants)
            {
                this.numPowerPlants = numPowerPlants;
                this.numAdvancedPowerPlants = numAdvancedPowerPlants;
                return this;
            }

            public Builder AddDefensiveBuilding()
            {
                this.numDefensiveBuildings++;
                return this;
            }

            public Builder SetNumDefensiveBuildings(int numDefensiveBuildings)
            {
                this.numDefensiveBuildings = numDefensiveBuildings;
                return this;
            }

            public Builder AddInfantry()
            {
                this.numInfantryUnits++;
                return this;
            }

            public Builder AddVehicle()
            {
                this.numVehicleUnits++;
                return this;
            }

            public Builder AddAircraft()
            {
                this.numAircraftUnits++;
                return this;
            }

            public Builder SetNumUnits(int numInfantryUnits, int numVehicleUnits, int numAircraftUnits)
            {
                this.numInfantryUnits = numInfantryUnits;
                this.numVehicleUnits = numVehicleUnits;
                this.numAircraftUnits = numAircraftUnits;
                return this;
            }

            public Builder AddOreRefinery()
            {
                this.numOreRefineries++;
                return this;
            }

            public Builder SetNumOreRefineries(int numOreRefineries)
            {
                this.numOreRefineries = numOreRefineries;
                return this;
            }

            public Builder AddGenericBuilding()
            {
                this.numOtherBuildings++;
                return this;
            }

        }
    }
}
