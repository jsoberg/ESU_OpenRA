using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.AI.Esu.Database;
using OpenRA.Mods.Common.AI.Esu.Geometry;

namespace OpenRA.Mods.Common.AI.Esu.Strategy.Scouting
{
    public class ScoutReportLocationGrid
    {
        // Size of a given cell in the report grid.
        public const int WIDTH_PER_GRID_SQUARE = 10;

        // Number of ticks to wait between scout report data updates.
        private const int TICKS_UNTIL_REPORT_DATABASE_UPDATE = 1000;

        // How many ticks before scout report times out and is thrown away.
        private const int TICK_TIMEOUT = 3000;

        private readonly int GridWidth;
        private readonly int GridHeight;
        private readonly List<ScoutReport>[][] ScoutReportGridMatrix;
        private readonly ScoutReportDataTable ScoutReportDataTable;

        public ScoutReportLocationGrid(World world)
        {
            this.GridWidth = GetRoundedIntDividedByCellSize(world.Map.MapSize.X);
            this.GridHeight = GetRoundedIntDividedByCellSize(world.Map.MapSize.Y);

            this.ScoutReportGridMatrix = BuildScoutReportGridMatrix();
            this.ScoutReportDataTable = new ScoutReportDataTable();
        }

        private List<ScoutReport>[][] BuildScoutReportGridMatrix()
        {
            List<ScoutReport>[][] grid = new List<ScoutReport>[GridWidth][];
            for (int i = 0; i < GridWidth; i++)
            {
                grid[i] = new List<ScoutReport>[GridHeight];
            }
            return grid;
        }

        public void AddScoutReportForActor(Actor actor, ScoutReport report)
        {
            CPos scoutPosition = actor.Location;
            ScoutReportGridMatrix.Count();

            int x = GetRoundedIntDividedByCellSize(scoutPosition.X);
            x = Normalize(x, GridWidth - 1);
            int y = GetRoundedIntDividedByCellSize(scoutPosition.Y);
            y = Normalize(y, GridHeight - 1);

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

        public void PerformUpdates(World world)
        {
            // Only log current scout report data every specified number of ticks.
            if ((world.GetCurrentLocalTickCount() % TICKS_UNTIL_REPORT_DATABASE_UPDATE) == 0) {
                LogCurrentScoutReportDataToDatabase();
            }
            RemoveDeadReports(world);
        }

        private void LogCurrentScoutReportDataToDatabase()
        {
            BestScoutReportData data = GetCurrentScoutReportData();
            if (data == null) {
                return;
            }

            ScoutReportDataTable.InsertScoutReportData(data);
        }

        private BestScoutReportData GetCurrentScoutReportData()
        {
            bool hasData = false;
            BestScoutReportData.Builder builder = new BestScoutReportData.Builder();

            for (int i = 0; i < ScoutReportGridMatrix.Count(); i++)
            {
                List<ScoutReport>[] row = ScoutReportGridMatrix[i];
                for (int j = 0; j < row.Count(); j++)
                {
                    List<ScoutReport> reports = row[j];
                    if (reports != null && reports.Count > 0) {
                        hasData = true;
                        AddDataToBuilder(builder, reports);
                    }
                }
            }

            return hasData ? builder.Build() : null;
        }

        private void AddDataToBuilder(BestScoutReportData.Builder builder, List<ScoutReport> reports)
        {
            foreach (ScoutReport report in reports)
            {
                builder.addRiskValue(report.ResponseRecommendation.RiskValue)
                    .addRewardValue(report.ResponseRecommendation.RewardValue);
            }
        }

        private void RemoveDeadReports(World world)
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

        public AggregateScoutReportData GetBestSurroundingCell(CPos cell)
        {
            if (cell == CPos.Invalid) {
                return null;
            }

            int cellGridPosX = cell.X / WIDTH_PER_GRID_SQUARE;
            int cellGridPosY = cell.Y / WIDTH_PER_GRID_SQUARE;

            int startPosX = (cellGridPosX - 1 < 0) ? cellGridPosX : cellGridPosX - 1;
            int startPosY = (cellGridPosY - 1 < 0) ? cellGridPosY : cellGridPosY - 1;
            int endPosX = (cellGridPosX + 1 > GridWidth - 1) ? cellGridPosX : cellGridPosX + 1;
            int endPosY = (cellGridPosY + 1 > GridHeight - 1) ? cellGridPosY : cellGridPosY + 1;

            AggregateScoutReportData best = null;
            for (int rowNum = startPosX; rowNum <= endPosX; rowNum++)
            {
                for (int colNum = startPosY; colNum <= endPosY; colNum++)
                {
                    AggregateScoutReportData cellData = GetAggregateDataForCell(rowNum, colNum);
                    if (best == null) {
                        best = cellData;
                    } else {
                        if (cellData != null) {
                            best = (cellData.AverageRewardValue >= best.AverageRewardValue && cellData.AverageRiskValue <= best.AverageRiskValue) ? cellData : best;
                        }
                    }
                }
            }
            return best;
        }

        public CPos GetSafeCellPositionInbetweenCells(CPos cell, CPos startPosition)
        {
            if (cell == CPos.Invalid) {
                return CPos.Invalid;
            }

            int cellGridPosX = cell.X / WIDTH_PER_GRID_SQUARE;
            int cellGridPosY = cell.Y / WIDTH_PER_GRID_SQUARE;

            int startPosX = (cellGridPosX - 1 < 0) ? cellGridPosX : cellGridPosX - 1;
            int startPosY = (cellGridPosY - 1 < 0) ? cellGridPosY : cellGridPosY - 1;
            int endPosX = (cellGridPosX + 1 > GridWidth - 1) ? cellGridPosX : cellGridPosX + 1;
            int endPosY = (cellGridPosY + 1 > GridHeight - 1) ? cellGridPosY : cellGridPosY + 1;

            List<CPos> possiblePositions = new List<CPos>();
            for (int rowNum = startPosX; rowNum <= endPosX; rowNum++)
            {
                for (int colNum = startPosY; colNum <= endPosY; colNum++)
                {
                    AggregateScoutReportData cellData = GetAggregateDataForCell(rowNum, colNum);
                    if (cellData == null || cellData.AverageRiskValue == 0) {
                        CPos position = new CPos(rowNum * WIDTH_PER_GRID_SQUARE, colNum * WIDTH_PER_GRID_SQUARE);
                        possiblePositions.Add(position);
                    }
                }
            }

            // Get the closest safe position to the start position.
            return GeometryUtils.GetPositionClosestToStart(startPosition, possiblePositions);
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
                builder.addResponseRecommendation(report.ResponseRecommendation);
            }
            return builder.Build();
        }

        private int GetRoundedIntDividedByCellSize(int pos)
        {
            return (int) Math.Round((double) pos / (double) WIDTH_PER_GRID_SQUARE);
        }

        // Make sure value is inbetween 0 and max
        private int Normalize(int value, int max)
        {
            value = Math.Min(value, max);
            return Math.Max(value, 0);
        }

        public BestScoutReportData GetBestScoutReportDataFromDatabase()
        {
            return ScoutReportDataTable.QueryForBestScoutReportData();
        }
    }
}
