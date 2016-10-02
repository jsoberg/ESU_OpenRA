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
        ///  The recommended response reward and risk for this report.
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

    public class ResponseRecommendation
    {
        public readonly Builder UnitInfoBuilder;
        public readonly int RewardValue;
        public readonly int RiskValue;

        public ResponseRecommendation(Builder builder)
        {
            this.RewardValue = ComputeRewardValue(builder);
            this.RiskValue = ComputeRiskValue(builder, RewardValue);
        }

        private int ComputeRewardValue(Builder builder)
        {
            return PowerPlants(builder) + OreRefineries(builder) + OtherBuildings(builder);
        }

        // ========================================
        // Reward Methods
        // ========================================

        private int PowerPlants(Builder builder)
        {
            return (builder.numPowerPlants + (2 * builder.numPowerPlants)) * builder.info.ScoutRecommendationImportanceMultiplier;
        }

        private int OreRefineries(Builder builder)
        {
            return builder.numOreRefineries * builder.info.ScoutRecommendationImportanceMultiplier;
        }

        private int OtherBuildings(Builder builder)
        {
            return builder.numOtherBuildings * builder.info.ScoutRecommendationImportanceMultiplier;
        }

        // ========================================
        // Risk Methods
        // ========================================

        private int ComputeRiskValue(Builder builder, int responseRecommendation)
        {
            return UnitsAndDefensiveStructures(builder);
        }

        private int UnitsAndDefensiveStructures(Builder builder)
        {
            return (builder.AllUnits() + builder.numDefensiveBuildings) * builder.info.ScoutRecommendationImportanceMultiplier;
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
