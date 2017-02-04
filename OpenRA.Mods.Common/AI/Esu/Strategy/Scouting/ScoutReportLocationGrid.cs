using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using OpenRA.Mods.Common.AI.Esu.Database;
using OpenRA.Mods.Common.AI.Esu.Geometry;

namespace OpenRA.Mods.Common.AI.Esu.Strategy.Scouting
{
    public class ScoutReportLocationGrid : UpdateListener
    {
        // Size of a given cell in the report grid.
        public const int WIDTH_PER_GRID_SQUARE = 10;

        // Number of ticks to wait between scout report data updates.
        private const int TICKS_UNTIL_REPORT_DATABASE_UPDATE = 1000;

        private readonly int GridWidth;
        private readonly int GridHeight;
        private readonly ScoutReportDataTable ScoutReportDataTable;
        private readonly ScoutReportLocationGridUpdateThread ScoutReportUpdateThread;

        private readonly object ScoutReportGridMatrixLock = new object();
        private List<ScoutReport>[][] ScoutReportGridMatrix;
        private AggregateScoutReportData BestCellData;

        private readonly object BestScoutReportDataLock = new object();
        private BestScoutReportData CachedBestScoutReportData;

        public ScoutReportLocationGrid(World world)
        {
            this.GridWidth = GetRoundedIntDividedByCellSize(world.Map.MapSize.X);
            this.GridHeight = GetRoundedIntDividedByCellSize(world.Map.MapSize.Y);
            this.ScoutReportDataTable = new ScoutReportDataTable();

            this.ScoutReportUpdateThread = new ScoutReportLocationGridUpdateThread(this, world, GridWidth, GridHeight, WIDTH_PER_GRID_SQUARE);

            // Load the current best scout report data initially.
            ThreadPool.QueueUserWorkItem(t => ReloadBestScoutReportDataInBackground());
        }

        public void QueueScoutReport(ScoutReport report)
        {
            ScoutReportUpdateThread.QueueScoutReportForNextRecreation(report);
        }

        void UpdateListener.OnGridUpdated(List<ScoutReport>[][] scoutReportGridMatrix, AggregateScoutReportData bestCell)
        {
            lock (ScoutReportGridMatrixLock)
            {
                ScoutReportGridMatrix = scoutReportGridMatrix;
                BestCellData = bestCell;
            }
        }

        public void PerformUpdates(World world)
        {
            ScoutReportUpdateThread.Tick();

            // Only log current scout report data every specified number of ticks.
            if ((world.GetCurrentLocalTickCount() % TICKS_UNTIL_REPORT_DATABASE_UPDATE) == 0) {
                LogCurrentScoutReportDataToDatabase();
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

            lock (ScoutReportGridMatrixLock) {
                AggregateScoutReportData best = null;
                for (int rowNum = startPosX; rowNum <= endPosX; rowNum++) {
                    for (int colNum = startPosY; colNum <= endPosY; colNum++) {
                        AggregateScoutReportData cellData = ScoutReportLocationGridUtils.GetAggregateDataForCell(ScoutReportGridMatrix, WIDTH_PER_GRID_SQUARE, rowNum, colNum);
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

            lock (ScoutReportGridMatrixLock) {
                List<CPos> possiblePositions = new List<CPos>();
                for (int rowNum = startPosX; rowNum <= endPosX; rowNum++) {
                    for (int colNum = startPosY; colNum <= endPosY; colNum++) {
                        AggregateScoutReportData cellData = ScoutReportLocationGridUtils.GetAggregateDataForCell(ScoutReportGridMatrix, WIDTH_PER_GRID_SQUARE, rowNum, colNum);
                        if (cellData == null || cellData.AverageRiskValue == 0) {
                            CPos position = new CPos(rowNum * WIDTH_PER_GRID_SQUARE, colNum * WIDTH_PER_GRID_SQUARE);
                            possiblePositions.Add(position);
                        }
                    }
                }

                // Get the closest safe position to the start position.
                return GeometryUtils.GetPositionClosestToStart(startPosition, possiblePositions);
            }
        }

        public AggregateScoutReportData GetCurrentBestFitCell()
        {
            lock (ScoutReportGridMatrixLock) {
                return BestCellData;
            }
        }

        public AggregateScoutReportData GetCurrentBestFitCellExcludingPosition(CPos position)
        {
            lock (ScoutReportGridMatrixLock) {
                return ScoutReportLocationGridUtils.GetCurrentBestFitCellExcludingPosition(ScoutReportGridMatrix, WIDTH_PER_GRID_SQUARE, position);
            }
        }

        // ========================================
        // Best Scout Report Logging
        // ========================================

        private void LogCurrentScoutReportDataToDatabase()
        {
            lock (ScoutReportGridMatrixLock) {
                BestScoutReportData data = CompileCurrentBestScoutReportData();
                if (data == null) {
                    return;
                }

                ScoutReportDataTable.InsertScoutReportData(data);
                // Reload the cache since it may have changed.
                ThreadPool.QueueUserWorkItem(t => ReloadBestScoutReportDataInBackground());
            }
        }

        private BestScoutReportData CompileCurrentBestScoutReportData()
        {
            bool hasData = false;
            BestScoutReportData.Builder builder = new BestScoutReportData.Builder();

            for (int i = 0; i < ScoutReportGridMatrix.Count(); i++)
            {
                List<ScoutReport>[] row = ScoutReportGridMatrix[i];
                for (int j = 0; j < row.Count(); j++)
                {
                    List<ScoutReport> reports = row[j];
                    if (reports != null && reports.Count > 0)
                    {
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

        public BestScoutReportData GetBestScoutReportDataFromDatabase()
        {
            lock (BestScoutReportDataLock)
            {
                return CachedBestScoutReportData;
            }
        }

        // ========================================
        // Background Threading Methods
        // ========================================

        private void ReloadBestScoutReportDataInBackground()
        {
            BestScoutReportData data = ScoutReportDataTable.QueryForBestScoutReportData();
            lock (BestScoutReportDataLock)
            {
                CachedBestScoutReportData = data; 
            }
        }

        // ========================================
        // Convenience Methods
        // ========================================

        private int GetRoundedIntDividedByCellSize(int pos)
        {
            return (int)Math.Round((double)pos / (double) WIDTH_PER_GRID_SQUARE);
        }
    }
}
