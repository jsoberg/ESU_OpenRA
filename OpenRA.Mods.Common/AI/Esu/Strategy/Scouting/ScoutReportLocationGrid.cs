using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.AI.Esu.Database;

namespace OpenRA.Mods.Common.AI.Esu.Strategy.Scouting
{
    public class ScoutReportLocationGrid
    {
        // Size of a given cell in the report grid.
        private const int WIDTH_PER_GRID_SQUARE = 10;

        // How many ticks before scout report times out and is thrown away.
        private const int TICK_TIMEOUT = 3000;

        private readonly List<ScoutReport>[][] ScoutReportGridMatrix;
        private readonly ScoutReportDataTable ScoutReportDataTable;

        public ScoutReportLocationGrid(World world)
        {
            this.ScoutReportGridMatrix = BuildScoutReportGridMatrix(world);
            this.ScoutReportDataTable = new ScoutReportDataTable();
        }

        private List<ScoutReport>[][] BuildScoutReportGridMatrix(World world)
        {
            int gridWidth = GetRoundedIntDividedByWidth(world.Map.MapSize.X);
            int gridHeight = GetRoundedIntDividedByWidth(world.Map.MapSize.Y);

            List<ScoutReport>[][] grid = new List<ScoutReport>[gridWidth][];
            for (int i = 0; i < gridWidth; i++)
            {
                grid[i] = new List<ScoutReport>[gridHeight];
            }
            return grid;
        }

        public void AddScoutReportForActor(Actor actor, ScoutReport report)
        {
            CPos scoutPosition = actor.Location;
            int x = GetRoundedIntDividedByWidth(scoutPosition.X);
            int y = GetRoundedIntDividedByWidth(scoutPosition.Y);

            List<ScoutReport> reportsForLocation = ScoutReportGridMatrix[x][y];
            if (reportsForLocation == null)
            {
                reportsForLocation = new List<ScoutReport>();
                ScoutReportGridMatrix[x][y] = reportsForLocation;
            }

            Log.Write("scout_report", "Report; Risk: {0}, Reward: {1} | Map X: {2}, Map Y {3} | Grid X: {4}, Grid Y: {5}".F(
                report.ResponseRecommendation.RiskValue, report.ResponseRecommendation.RewardValue, scoutPosition.X, scoutPosition.Y, x, y));
            reportsForLocation.Add(report);
        }

        public void RemoveDeadReports(World world)
        {
            for (int i = 0; i < ScoutReportGridMatrix.Count(); i++)
            {
                List<ScoutReport>[] row = ScoutReportGridMatrix[i];
                for (int j = 0; j < row.Count(); j++)
                {
                    List<ScoutReport> report = row[j];
                    if (report != null)
                    {
                        report.RemoveAll(sr => (sr.TickReported + TICK_TIMEOUT) <= world.GetCurrentLocalTickCount());
                    }
                }
            }
        }

        public AggregateScoutReportData GetCurrentBestFitCell()
        {
            AggregateScoutReportData best = null;

            for (int i = 0; i < ScoutReportGridMatrix.Count(); i++)
            {
                List<ScoutReport>[] row = ScoutReportGridMatrix[i];
                for (int j = 0; j < row.Count(); j++)
                {
                    AggregateScoutReportData current = GetAggregateDataForCell(i, j);
                    if (best == null || (current != null && (current.CompareTo(best) > 0)))
                    {
                        best = current;
                    }
                }
            }

            return best;
        }

        public AggregateScoutReportData GetAggregateDataForCell(int X, int Y)
        {
            List<ScoutReport> cell = ScoutReportGridMatrix[X][Y];
            if (cell == null || cell.Count() == 0)
            {
                return null;
            }

            CPos pos = new CPos(X * WIDTH_PER_GRID_SQUARE, Y * WIDTH_PER_GRID_SQUARE);
            AggregateScoutReportData.Builder builder = new AggregateScoutReportData.Builder()
                .withNumReports(cell.Count())
                .withRelativePosition(pos);

            foreach (ScoutReport report in cell)
            {
                builder.addRiskValue(report.ResponseRecommendation.RiskValue)
                    .addRewardValue(report.ResponseRecommendation.RewardValue);
            }
            return builder.Build();
        }

        private int GetRoundedIntDividedByWidth(int pos)
        {
            return (int)Math.Round((double)pos / (double)WIDTH_PER_GRID_SQUARE);
        }
    }
}
