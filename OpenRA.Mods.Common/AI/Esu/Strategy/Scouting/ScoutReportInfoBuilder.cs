using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRA.Mods.Common.AI.Esu.Strategy.Scouting
{
    public class ScoutReportInfoBuilder
    {
        public readonly EsuAIInfo info;

        public int numPowerPlants;
        public int numAdvancedPowerPlants;

        public int numAntiInfantryDefense;
        public int numAntiVehicleDefense;
        public int numAntiAirDefense;
        public int numOtherDefensiveBuildings;

        public int numInfantryUnits;
        public int numVehicleUnits;
        public int numAircraftUnits;

        public int numOreRefineries;

        public int numOtherBuildings;

        public ScoutReportInfoBuilder(EsuAIInfo info)
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

        public ScoutReportInfoBuilder AddPowerPlant()
        {
            this.numPowerPlants++;
            return this;
        }

        public ScoutReportInfoBuilder AddAdvancedPowerPlant()
        {
            this.numAdvancedPowerPlants++;
            return this;
        }

        public ScoutReportInfoBuilder SetNumPowerPlants(int numPowerPlants, int numAdvancedPowerPlants)
        {
            this.numPowerPlants = numPowerPlants;
            this.numAdvancedPowerPlants = numAdvancedPowerPlants;
            return this;
        }

        public ScoutReportInfoBuilder AddAntiInfantryDefensiveBuilding()
        {
            this.numAntiInfantryDefense++;
            return this;
        }

        public ScoutReportInfoBuilder AddAntiVehicleDefensiveBuilding()
        {
            this.numAntiVehicleDefense++;
            return this;
        }

        public ScoutReportInfoBuilder AddAntiAirDefensiveBuilding()
        {
            this.numAntiAirDefense++;
            return this;
        }

        public ScoutReportInfoBuilder AddOtherDefensiveBuilding()
        {
            this.numOtherDefensiveBuildings++;
            return this;
        }

        public ScoutReportInfoBuilder AddInfantry()
        {
            this.numInfantryUnits++;
            return this;
        }

        public ScoutReportInfoBuilder AddVehicle()
        {
            this.numVehicleUnits++;
            return this;
        }

        public ScoutReportInfoBuilder AddAircraft()
        {
            this.numAircraftUnits++;
            return this;
        }

        public ScoutReportInfoBuilder SetNumUnits(int numInfantryUnits, int numVehicleUnits, int numAircraftUnits)
        {
            this.numInfantryUnits = numInfantryUnits;
            this.numVehicleUnits = numVehicleUnits;
            this.numAircraftUnits = numAircraftUnits;
            return this;
        }

        public ScoutReportInfoBuilder AddOreRefinery()
        {
            this.numOreRefineries++;
            return this;
        }

        public ScoutReportInfoBuilder SetNumOreRefineries(int numOreRefineries)
        {
            this.numOreRefineries = numOreRefineries;
            return this;
        }

        public ScoutReportInfoBuilder AddGenericBuilding()
        {
            this.numOtherBuildings++;
            return this;
        }

    }
}
