using System;

namespace OpenRA.Mods.Common.AI.Esu.Strategy.Scouting
{
    public class AggregateScoutReportData : IComparable<AggregateScoutReportData>
    {
        public readonly int NumReports;

        public readonly int AverageRiskValue;
        public readonly int AverageRewardValue;
        public readonly AggregateOffenseDefenseCellData OffenseDefenseCellData;

        public readonly CPos RelativePosition;

        private AggregateScoutReportData(int numReports, int averageRiskValue, int averageRewardValue, AggregateOffenseDefenseCellData offenseDefenseCellData, CPos relativePosition)
        {
            this.NumReports = numReports;
            this.AverageRiskValue = averageRiskValue;
            this.AverageRewardValue = averageRewardValue;
            this.OffenseDefenseCellData = offenseDefenseCellData;
            this.RelativePosition = relativePosition;
        }

        public int CompareTo(AggregateScoutReportData other)
        {
            float fit = (AverageRiskValue != 0) ? (AverageRewardValue / AverageRiskValue) : AverageRewardValue;
            float otherFit = (other.AverageRiskValue != 0) ? (other.AverageRewardValue / other.AverageRiskValue) : other.AverageRewardValue;
            return fit.CompareTo(otherFit);
        }

        public class Builder
        {
            private int NumReports;
            private int TotalRiskValue;
            private int TotalRewardValue;
            private CPos RelativePosition;

            private int NumInfantry;
            private int NumVehicles;
            private int NumAir;

            private int NumAntiInfantry;
            private int NumAntiVehicle;
            private int NumAntiAir;

            public Builder withNumReports(int numReports)
            {
                this.NumReports = numReports;
                return this;
            }

            public Builder withRelativePosition(CPos RelativePosition)
            {
                this.RelativePosition = RelativePosition;
                return this;
            }

            public Builder addResponseRecommendation(ResponseRecommendation recommendation)
            {
                addRiskValue(recommendation.RiskValue);
                addRewardValue(recommendation.RewardValue);
                addInfoBuilder(recommendation.InfoBuilder);
                return this;
            }

            public Builder addRiskValue(int riskValue)
            {
                TotalRiskValue += riskValue;
                return this;
            }

            public Builder addRewardValue(int rewardValue)
            {
                TotalRewardValue += rewardValue;
                return this;
            }

            public Builder addInfoBuilder(ScoutReportInfoBuilder builder)
            {
                NumInfantry += builder.NumInfantryUnits;
                NumVehicles += builder.NumVehicleUnits;
                NumAir = builder.NumAircraftUnits;

                NumAntiInfantry += builder.NumAntiInfantryDefense;
                NumAntiVehicle += builder.NumAntiVehicleDefense;
                NumAntiAir += builder.NumAntiAirDefense;

                return this;
            }

            public AggregateScoutReportData Build()
            {
                AggregateOffenseDefenseCellData data = buildAggregateOffenseDefenseCellData();
                return new AggregateScoutReportData(NumReports, (TotalRiskValue / NumReports), (TotalRewardValue / NumReports), data,  RelativePosition);
            }

            private AggregateOffenseDefenseCellData buildAggregateOffenseDefenseCellData()
            {
                double unitTotal = (NumInfantry + NumVehicles + NumAir);
                double infantryPercentage = NumInfantry / unitTotal;
                double vehiclePercentage = NumVehicles / unitTotal;
                double airPercentage = NumAir / unitTotal;

                double defenseTotal = (NumAntiInfantry + NumAntiVehicle + NumAntiAir);
                double antiInfantryPercentage = NumAntiInfantry / defenseTotal;
                double antiVehiclePercentage = NumAntiVehicle / defenseTotal;
                double antiAirPercentage = NumAntiAir / defenseTotal;

                return new AggregateOffenseDefenseCellData(
                    infantryPercentage,
                    vehiclePercentage,
                    airPercentage,
                    
                    antiInfantryPercentage,
                    antiVehiclePercentage,
                    antiAirPercentage 
                );
            }
        }

        public class AggregateOffenseDefenseCellData
        {
            public readonly double InfantryUnitPercentage;
            public readonly double VehicleUnitPercentage;
            public readonly double AirUnitPercentage;

            public readonly double AntiInfantryDefensePercentage;
            public readonly double AntiVehicleDefensePercentage;
            public readonly double AntiAirDefensePercentage;

            // This constructor is an abomination, and should be a builder.
            public AggregateOffenseDefenseCellData(
                double infantryUnitPercentage, 
                double vehicleUnitPercentage,
                double airUnitPercentage,
                
                double antiInfantryDefensePercentage,
                double antiVehicleDefensePercentage,
                double antiAirDefensePercentage)
            {
                this.InfantryUnitPercentage = infantryUnitPercentage;
                this.VehicleUnitPercentage = vehicleUnitPercentage;
                this.AirUnitPercentage = airUnitPercentage;

                this.AntiInfantryDefensePercentage = antiInfantryDefensePercentage;
                this.AntiVehicleDefensePercentage = antiVehicleDefensePercentage;
                this.AntiAirDefensePercentage = antiAirDefensePercentage;
            }
        }
    }
}
