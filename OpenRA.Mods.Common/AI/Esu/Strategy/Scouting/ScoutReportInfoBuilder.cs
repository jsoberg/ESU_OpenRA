using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRA.Mods.Common.AI.Esu.Strategy.Scouting
{
    public class ScoutReportInfoBuilder
    {
        public readonly EsuAIInfo info;

        public int NumPowerPlants;
        public int NumAdvancedPowerPlants;

        public int NumAntiInfantryDefense;
        public int NumAntiVehicleDefense;
        public int NumAntiAirDefense;
        public int NumOtherDefensiveBuildings;

        public int NumInfantryUnits;
        public int NumVehicleUnits;
        public int NumAircraftUnits;

        public int NumOreRefineries;

        public int NumOtherBuildings;

        public ScoutReportInfoBuilder(EsuAIInfo info)
        {
            this.info = info;
        }

        public int AllUnits()
        {
            return (NumAircraftUnits + NumInfantryUnits + NumVehicleUnits);
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

        public ScoutReportInfoBuilder AddAntiInfantryDefensiveBuilding()
        {
            this.NumAntiInfantryDefense++;
            return this;
        }

        public ScoutReportInfoBuilder AddAntiVehicleDefensiveBuilding()
        {
            this.NumAntiVehicleDefense++;
            return this;
        }

        public ScoutReportInfoBuilder AddAntiAirDefensiveBuilding()
        {
            this.NumAntiAirDefense++;
            return this;
        }

        public ScoutReportInfoBuilder AddOtherDefensiveBuilding()
        {
            this.NumOtherDefensiveBuildings++;
            return this;
        }

        public ScoutReportInfoBuilder AddInfantry()
        {
            this.NumInfantryUnits++;
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

        public ScoutReportInfoBuilder SetNumUnits(int numInfantryUnits, int numVehicleUnits, int numAircraftUnits)
        {
            this.NumInfantryUnits = numInfantryUnits;
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
