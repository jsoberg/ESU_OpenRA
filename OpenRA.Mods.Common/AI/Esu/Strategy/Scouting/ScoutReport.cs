using System.Collections.Generic;

namespace OpenRA.Mods.Common.AI.Esu.Strategy.Scouting
{
    public class ScoutReport
    {
        /// <summary>
        ///  The recommended response reward and risk for this report.
        /// </summary>
        public readonly ResponseRecommendation ResponseRecommendation;
        public WPos ReportedPosition { get; internal set;  }
        public long TickReported { get; internal set; }

        public ScoutReport(ResponseRecommendation response, WPos currentPosition, World world)
        {
            this.ResponseRecommendation = response;
            this.ReportedPosition = currentPosition;

            this.TickReported = world.GetCurrentLocalTickCount();
        }

        private class Comparator : IComparer<ScoutReport>
        {
            int IComparer<ScoutReport>.Compare(ScoutReport x, ScoutReport y)
            {
                return (int) (y.TickReported - x.TickReported);
            }
        }

        public static readonly IComparer<ScoutReport> ScoutReportComparator = new Comparator();
    }

    public class ResponseRecommendation
    {
        public readonly int RewardValue;
        public readonly int RiskValue;

        public readonly ScoutReportInfoBuilder InfoBuilder;

        public ResponseRecommendation(ScoutReportInfoBuilder builder)
        {
            this.RewardValue = ComputeRewardValue(builder);
            this.RiskValue = ComputeRiskValue(builder, RewardValue);

            this.InfoBuilder = builder;
        }

        private int ComputeRewardValue(ScoutReportInfoBuilder builder)
        {
            return PowerPlants(builder) + OreRefineries(builder) + OtherBuildings(builder);
        }

        // ========================================
        // Reward Methods
        // ========================================

        private int PowerPlants(ScoutReportInfoBuilder builder)
        {
            return (int) ((builder.NumPowerPlants + (2 * builder.NumAdvancedPowerPlants)) * builder.info.GetScoutRecommendationImportanceMultiplier());
        }

        private int OreRefineries(ScoutReportInfoBuilder builder)
        {
            return (int)(builder.NumOreRefineries * builder.info.GetScoutRecommendationImportanceMultiplier());
        }

        private int OtherBuildings(ScoutReportInfoBuilder builder)
        {
            return (int)(builder.NumOtherBuildings * builder.info.GetScoutRecommendationImportanceMultiplier());
        }

        // ========================================
        // Risk Methods
        // ========================================

        private int ComputeRiskValue(ScoutReportInfoBuilder builder, int responseRecommendation)
        {
            return UnitsAndDefensiveStructures(builder);
        }

        private int UnitsAndDefensiveStructures(ScoutReportInfoBuilder builder)
        {
            return (int) ((builder.AllUnits() + builder.AllDefensiveStructures()) * builder.info.GetScoutRecommendationImportanceMultiplier());
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
    }
}
