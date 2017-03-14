using System.Collections.Generic;
using OpenRA.Mods.Common.AI.Esu.Rules.Units;

namespace OpenRA.Mods.Common.AI.Esu.Strategy.Scouting
{
    public class ScoutReport
    {
        /// <summary>
        ///  The recommended response reward and risk for this report.
        /// </summary>
        public readonly ResponseRecommendation ResponseRecommendation;
        public readonly CPos ReportedCPosition;
        public readonly WPos ReportedWPosition;
        public readonly long TickReported;

        public ScoutReport(ResponseRecommendation response, CPos currentCPosition, WPos currentWPosition, World world)
        {
            this.ResponseRecommendation = response;
            this.ReportedCPosition = currentCPosition;
            this.ReportedWPosition = currentWPosition;

            this.TickReported = world.GetCurrentLocalTickCount();
        }

        /** @return true if buildings are part of this report, false otherwise. */
        public bool IsStaticReport()
        {
            return (ResponseRecommendation.InfoBuilder.AllBuildings() + ResponseRecommendation.InfoBuilder.AllDefensiveStructures()) > 0;
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

        public ResponseRecommendation(ScoutReportInfoBuilder builder, CompiledUnitDamageStatistics stats)
        {
            this.RewardValue = ComputeRewardValue(builder);
            this.RiskValue = ComputeRiskValue(builder, stats);

            this.InfoBuilder = builder;
        }

        private int ComputeRewardValue(ScoutReportInfoBuilder builder)
        {
            return Harvesters(builder) + PowerPlants(builder) + OreRefineries(builder) + OtherBuildings(builder);
        }

        // ========================================
        // Reward Methods
        // ========================================

        private int Harvesters(ScoutReportInfoBuilder builder)
        {
            return (int)((builder.NumHarvesters) * builder.Info.GetHarvesterScoutRecommendationImportanceMultiplier());
        }

        private int PowerPlants(ScoutReportInfoBuilder builder)
        {
            return (int) ((builder.NumPowerPlants + (2 * builder.NumAdvancedPowerPlants)));
        }

        private int OreRefineries(ScoutReportInfoBuilder builder)
        {
            return (int)(builder.NumOreRefineries);
        }

        private int OtherBuildings(ScoutReportInfoBuilder builder)
        {
            return (int)(builder.NumOtherBuildings);
        }

        // ========================================
        // Risk Methods
        // ========================================

        private int ComputeRiskValue(ScoutReportInfoBuilder builder, CompiledUnitDamageStatistics stats)
        {
            return UnitsAndDefensiveStructures(builder, stats);
        }

        private int UnitsAndDefensiveStructures(ScoutReportInfoBuilder builder, CompiledUnitDamageStatistics stats)
        {
            return DefensiveStructuresModifiedRisk(builder, stats);
        }

        private int DefensiveStructuresModifiedRisk(ScoutReportInfoBuilder builder, CompiledUnitDamageStatistics stats)
        {
            int risk = 0;
            foreach (KeyValuePair<string, int> entry in builder.DefensiveStructureCounts) {
                int modifier = stats != null ? stats.GetDamageModifierForActorSubset(entry.Key, 
                    EsuAIConstants.Defense.VALUES) : 1;
                risk += (modifier * entry.Value);
            }
            return risk;
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
