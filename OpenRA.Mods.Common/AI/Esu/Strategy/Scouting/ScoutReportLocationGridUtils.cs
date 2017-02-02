using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRA.Mods.Common.AI.Esu.Strategy.Scouting
{
    public static class ScoutReportLocationGridUtils
    {
        public static AggregateScoutReportData GetCurrentBestFitCell(List<ScoutReport>[][] ScoutReportGridMatrix, int widthPerGridSquare)
        {
            return GetCurrentBestFitCellExcludingPosition(ScoutReportGridMatrix, widthPerGridSquare, CPos.Invalid);
        }

        public static AggregateScoutReportData GetCurrentBestFitCellExcludingPosition(List<ScoutReport>[][] ScoutReportGridMatrix, int widthPerGridQuare, CPos excludingPosition)
        {
            int x = excludingPosition != CPos.Invalid ? GetRoundedIntDividedByCellSize(widthPerGridQuare, excludingPosition.X) : -1;
            int y = excludingPosition != CPos.Invalid ? GetRoundedIntDividedByCellSize(widthPerGridQuare, excludingPosition.Y) : -1;

            AggregateScoutReportData best = null;

            for (int i = 0; i < ScoutReportGridMatrix.Count(); i++)
            {
                List<ScoutReport>[] row = ScoutReportGridMatrix[i];
                for (int j = 0; j < row.Count(); j++)
                {
                    if (i == x && j == y)
                    {
                        continue;
                    }

                    AggregateScoutReportData current = GetAggregateDataForCell(ScoutReportGridMatrix, widthPerGridQuare, i, j);
                    if (best == null || (current != null && (current.CompareTo(best) > 0)))
                    {
                        best = current;
                    }
                }
            }

            return best;
        }

        public static AggregateScoutReportData GetAggregateDataForCell(List<ScoutReport>[][] ScoutReportGridMatrix, int widthPerGridSquare, int X, int Y)
        {
            List<ScoutReport> cell = ScoutReportGridMatrix[X][Y];
            if (cell == null || cell.Count() == 0)
            {
                return null;
            }

            CPos pos = new CPos(X * widthPerGridSquare, Y * widthPerGridSquare);
            AggregateScoutReportData.Builder builder = new AggregateScoutReportData.Builder()
                .withNumReports(cell.Count())
                .withRelativePosition(pos);

            foreach (ScoutReport report in cell)
            {
                builder.addResponseRecommendation(report.ResponseRecommendation);
            }
            return builder.Build();
        }

        private static int GetRoundedIntDividedByCellSize(int widthPerGridSquare, int pos)
        {
            return (int)Math.Round((double)pos / (double)widthPerGridSquare);
        }

        // Make sure value is inbetween 0 and max
        private static int Normalize(int value, int max)
        {
            value = Math.Min(value, max);
            return Math.Max(value, 0);
        }
    }
}
