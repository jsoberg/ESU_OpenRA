using System;

namespace OpenRA.Mods.Common.AI.Esu.Strategy.Scouting
{
    public class AggregateScoutReportData : IComparable<AggregateScoutReportData>
    {
        public readonly int NumReports;

        public readonly int AverageRiskValue;
        public readonly int AverageRewardValue;

        public readonly CPos RelativePosition;

        private AggregateScoutReportData(int numReports, int averageRiskValue, int averageRewardValue, CPos relativePosition)
        {
            this.NumReports = numReports;
            this.AverageRiskValue = averageRiskValue;
            this.AverageRewardValue = averageRewardValue;
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

            public AggregateScoutReportData Build()
            {
                return new AggregateScoutReportData(NumReports, (TotalRiskValue / NumReports), (TotalRewardValue / NumReports), RelativePosition);
            }
        }
    }
}
