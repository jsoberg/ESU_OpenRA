using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRA.Mods.Common.AI.Esu.Strategy.Scouting
{
    public class ScoutReportInfoBuilder
    {
        public readonly EsuAIInfo Info;

        public int NumPowerPlants;
        public int NumAdvancedPowerPlants;

        public int NumAntiInfantryDefense;
        public int NumAntiVehicleDefense;
        public int NumAntiAirDefense;
        public int NumOtherDefensiveBuildings;
        public readonly Dictionary<string, int> DefensiveStructureCounts;

        public int NumInfantryUnits;
        public int NumHarvesters;
        public int NumVehicleUnits;
        public int NumAircraftUnits;

        public int NumOreRefineries;

        public int NumOtherBuildings;

        public ScoutReportInfoBuilder(EsuAIInfo info)
        {
            this.Info = info;
            this.DefensiveStructureCounts = new Dictionary<string, int>();
        }

        public int AllOffensiveUnits()
        {
            return (NumAircraftUnits + NumInfantryUnits + NumVehicleUnits);
        }

        public int AllBuildings()
        {
            return (NumPowerPlants + NumAdvancedPowerPlants + NumOreRefineries + NumOtherBuildings);
        }

        public int AllDefensiveStructures()
        {
            return (NumAntiInfantryDefense + NumAntiVehicleDefense + NumAntiAirDefense + NumOtherDefensiveBuildings);
        }

        public ScoutReportInfoBuilder AddPowerPlant()
        {
            this.NumPowerPlants++;
            return this;
        }

        public ScoutReportInfoBuilder AddAdvancedPowerPlant()
        {
            this.NumAdvancedPowerPlants++;
            return this;
        }

        public ScoutReportInfoBuilder SetNumPowerPlants(int numPowerPlants, int numAdvancedPowerPlants)
        {
            this.NumPowerPlants = numPowerPlants;
            this.NumAdvancedPowerPlants = numAdvancedPowerPlants;
            return this;
        }

        public ScoutReportInfoBuilder AddDefensiveBuilding(string name)
        {
            if (DefensiveStructureCounts.ContainsKey(name)) {
                DefensiveStructureCounts[name] ++;
            } else {
                DefensiveStructureCounts[name] = 1;
            }

            if (EsuAIConstants.Defense.IsAntiInfantry(name)) {
                NumAntiInfantryDefense++;
            }
            if (EsuAIConstants.Defense.IsAntiVehicle(name)) {
                NumAntiVehicleDefense++;
            }
            if (EsuAIConstants.Defense.IsAntiAir(name)) {
                NumAntiAirDefense++;
            }
            NumOtherDefensiveBuildings++;
            return this;
        }

        public ScoutReportInfoBuilder AddInfantry()
        {
            this.NumInfantryUnits++;
            return this;
        }

        public ScoutReportInfoBuilder AddHarvester()
        {
            this.NumHarvesters++;
            return this;
        }

        public ScoutReportInfoBuilder AddVehicle()
        {
            this.NumVehicleUnits++;
            return this;
        }

        public ScoutReportInfoBuilder AddAircraft()
        {
            this.NumAircraftUnits++;
            return this;
        }

        public ScoutReportInfoBuilder SetNumUnits(int numInfantryUnits, int numHarvesters, int numVehicleUnits, int numAircraftUnits)
        {
            this.NumInfantryUnits = numInfantryUnits;
            this.NumHarvesters = numHarvesters;
            this.NumVehicleUnits = numVehicleUnits;
            this.NumAircraftUnits = numAircraftUnits;
            return this;
        }

        public ScoutReportInfoBuilder AddOreRefinery()
        {
            this.NumOreRefineries++;
            return this;
        }

        public ScoutReportInfoBuilder SetNumOreRefineries(int numOreRefineries)
        {
            this.NumOreRefineries = numOreRefineries;
            return this;
        }

        public ScoutReportInfoBuilder AddGenericBuilding()
        {
            this.NumOtherBuildings++;
            return this;
        }

    }
}
