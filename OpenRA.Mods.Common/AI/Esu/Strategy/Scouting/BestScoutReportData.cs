using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRA.Mods.Common.AI.Esu.Strategy.Scouting
{
    public class BestScoutReportData
    {
        public readonly int LowestRisk;
        public readonly int HighestRisk;
        public readonly int LowestReward;
        public readonly int HighestReward;

        private BestScoutReportData(int lowestRisk, int highestRisk, int lowestReward, int highestReward)
        {
            this.LowestRisk = lowestRisk;
            this.HighestRisk = highestRisk;
            this.LowestReward = lowestReward;
            this.HighestReward = highestReward;
        }

        public class Builder
        {
            private int LowestRisk = int.MaxValue;
            private int HighestRisk = int.MinValue;
            private int LowestReward = int.MaxValue;
            private int HighestReward = int.MinValue;

            public Builder addRiskValue(int riskValue)
            {
                LowestRisk = Math.Min(LowestRisk, riskValue);
                HighestRisk = Math.Max(HighestRisk, riskValue);

                return this;
            }

            public Builder addRewardValue(int rewardValue)
            {
                LowestReward = Math.Min(LowestReward, rewardValue);
                HighestReward = Math.Max(HighestReward, rewardValue);

                return this;
            }

            public BestScoutReportData Build()
            {
                return new BestScoutReportData(LowestRisk, HighestRisk, LowestReward, HighestReward);
            }
        }
    }
}
