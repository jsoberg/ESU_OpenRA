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
        public long LastRefreshTick { get; internal set; }

        public ScoutReport()
        {

        }

        public void UpdateForActor(Actor actor, World world, ResponseRecommendation recommendation)
        {
            int numRecommendations = 0;
            if (ActorToRecommendationMap.ContainsKey(actor)) {
                numRecommendations = ActorToRecommendationMap[actor];
            }
            numRecommendations++;
            ActorToRecommendationMap[actor] = numRecommendations;

            LastRefreshTick = world.GetCurrentLocalTickCount();
        }
    }

    public class ResponseRecommendation
    {
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
            return (builder.AllUnits() + builder.AllDefensiveStructures()) * builder.info.ScoutRecommendationImportanceMultiplier;
        }

        public static bool operator ==(ResponseRecommendation a, ResponseRecommendation b)
        {
            if (System.Object.ReferenceEquals(a, b)) {
                return true;
            }

            if (((object)a == null) || ((object)b == null)) {
                return false;
            }

            return a.RewardValue == b.RewardValue && a.RiskValue == b.RiskValue;
        }

        public static bool operator !=(ResponseRecommendation a, ResponseRecommendation b)
        {
            return !(a == b);
        }

        public override bool Equals(object obj)
        {
            return (this == (ResponseRecommendation) obj);
        }

        public override int GetHashCode()
        {
            int hash = 17;
            hash += RiskValue * 23;
            hash += RewardValue * 23;
            return hash;
        }

        public class Builder
        {
            internal readonly EsuAIInfo info;

            internal int numPowerPlants;
            internal int numAdvancedPowerPlants;

            internal int numAntiInfantryDefense;
            internal int numAntiVehicleDefense;
            internal int numAntiAirDefense;
            internal int numOtherDefensiveBuildings;

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

            public int AllDefensiveStructures()
            {
                return (numAntiInfantryDefense + numAntiVehicleDefense + numAntiAirDefense + numOtherDefensiveBuildings);
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

            public Builder AddAntiInfantryDefensiveBuilding()
            {
                this.numAntiInfantryDefense++;
                return this;
            }

            public Builder AddAntiVehicleDefensiveBuilding()
            {
                this.numAntiVehicleDefense++;
                return this;
            }

            public Builder AddAntiAirDefensiveBuilding()
            {
                this.numAntiAirDefense++;
                return this;
            }

            public Builder AddOtherDefensiveBuilding()
            {
                this.numOtherDefensiveBuildings++;
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
