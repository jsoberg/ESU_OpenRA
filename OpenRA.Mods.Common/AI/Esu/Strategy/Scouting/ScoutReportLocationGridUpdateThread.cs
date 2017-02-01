﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace OpenRA.Mods.Common.AI.Esu.Strategy.Scouting
{
    public class ScoutReportLocationGridUpdateThread
    {
        // How many ticks before more static scout reports (buidlings/defense) time out and are thrown away.
        private const int TickStaticReportTimeout = 2400;

        // How many ticks before more transient scout reports (units) time out and are thrown away.
        private const int TickTransientReportTimeout = 800;

        private const int NumTicksBeforeRecreation = 40;
        private int TicksSinceLastRecreation = 0;

        private readonly UpdateListener UpdateListener;
        private readonly int GridWidth;
        private readonly int GridHeight;
        private readonly int WidthPerGridSquare;
        private readonly World World;

        // Thread unsafe objects
        private readonly Queue<ScoutReport> QueuedReports = new Queue<ScoutReport>();
        private readonly List<ScoutReport>[][] CurrentScoutReportGridMatrix;
        private readonly object ReportQueueLock = new object();
        private readonly AutoResetEvent WaitHandle = new AutoResetEvent(false);
        private volatile bool IsShutdown;

        public ScoutReportLocationGridUpdateThread(UpdateListener listener, World world, int width, int height, int widthPerGridSquare)
        {
            this.UpdateListener = listener;
            this.World = world;
            this.GridWidth = width;
            this.GridHeight = height;
            this.WidthPerGridSquare = widthPerGridSquare;

            this.CurrentScoutReportGridMatrix = BuildScoutReportGridMatrix();

            var thread = new Thread(() => RunRecreateScoutReportMatrixThread());
            thread.Start();
        }

        private List<ScoutReport>[][] BuildScoutReportGridMatrix()
        {
            List<ScoutReport>[][] grid = new List<ScoutReport>[GridWidth][];
            for (int i = 0; i < GridWidth; i++) {
                grid[i] = new List<ScoutReport>[GridHeight];
            }
            return grid;
        }

        public void QueueScoutReportForNextRecreation(ScoutReport report)
        {
            lock (ReportQueueLock) {
                QueuedReports.Enqueue(report);
            }
        }

        public void Tick()
        {
            TicksSinceLastRecreation++;
            if (TicksSinceLastRecreation >= NumTicksBeforeRecreation) {
                // Signal thread to recreate.
                WaitHandle.Set();
                TicksSinceLastRecreation = 0;
            }
        }

        public void RunRecreateScoutReportMatrixThread()
        {
            while (!IsShutdown)
            {
                // Clone queued reports and free the queue up to continue to be added to.
                Queue<ScoutReport> currentQueuedReports = new Queue<ScoutReport>();
                lock (ReportQueueLock)
                {
                    while (QueuedReports.Count > 0)
                    {
                        currentQueuedReports.Enqueue(QueuedReports.Dequeue());
                    }
                }

                // Add queued reports to matrix.
                foreach (ScoutReport report in currentQueuedReports)
                {
                    AddScoutToMatrix(report);
                }

                // Remove any dead reports.
                RemoveDeadReports(World.GetCurrentLocalTickCount());

                // Clone matrix and send to listener.
                var clonedMatrix = Clone(CurrentScoutReportGridMatrix);
                var bestCell = ScoutReportLocationGridUtils.GetCurrentBestFitCell(clonedMatrix, WidthPerGridSquare);
                UpdateListener.OnGridUpdated(clonedMatrix, bestCell);

                // Wait until we are signaled to recreate again.
                WaitHandle.WaitOne();
            }
        }

        private void AddScoutToMatrix(ScoutReport report)
        {
            int x = GetRoundedIntDividedByCellSize(report.ReportedCPosition.X);
            x = Normalize(x, GridWidth - 1);
            int y = GetRoundedIntDividedByCellSize(report.ReportedCPosition.Y);
            y = Normalize(y, GridHeight - 1);

            List<ScoutReport> reportsForLocation = CurrentScoutReportGridMatrix[x][y];
            if (reportsForLocation == null)
            {
                reportsForLocation = new List<ScoutReport>();
                CurrentScoutReportGridMatrix[x][y] = reportsForLocation;
            }

            Log.Write("scout_report", "Report; Risk: {0}, Reward: {1} | Map X: {2}, Map Y {3} | Grid X: {4}, Grid Y: {5}".F(
                report.ResponseRecommendation.RiskValue, report.ResponseRecommendation.RewardValue, report.ReportedCPosition.X, report.ReportedCPosition.Y, x, y));
            reportsForLocation.Add(report);
        }

        private int GetRoundedIntDividedByCellSize(int pos)
        {
            return (int)Math.Round((double)pos / (double) WidthPerGridSquare);
        }

        // Make sure value is inbetween 0 and max
        private int Normalize(int value, int max)
        {
            value = Math.Min(value, max);
            return Math.Max(value, 0);
        }

        private void RemoveDeadReports(int currentTickCount)
        {
            for (int i = 0; i < CurrentScoutReportGridMatrix.Count(); i++)
            {

                List<ScoutReport>[] row = CurrentScoutReportGridMatrix[i];
                for (int j = 0; j < row.Count(); j++)
                {
                    List<ScoutReport> reports = row[j];
                    RemoveDeadReportsBasedOnTransiency(currentTickCount, reports);
                }
            }
        }

        private void RemoveDeadReportsBasedOnTransiency(int currentTickCount, List<ScoutReport> reports)
        {
            if (reports == null) {
                return;
            }

            for (int i = reports.Count - 1; i >= 0; i--) {
                ScoutReport report = reports[i];
                if (report.IsStaticReport()) {
                    if ((report.TickReported + TickStaticReportTimeout) <= currentTickCount) {
                        reports.RemoveAt(i);
                    }
                } else {
                    if ((report.TickReported + TickTransientReportTimeout) <= currentTickCount) {
                        reports.RemoveAt(i);
                    }
                }
            }
        }

        private List<ScoutReport>[][] Clone(List<ScoutReport>[][] CurrentScoutReportGridMatrix)
        {
            List<ScoutReport>[][] ClonedScoutReportGridMatrix = BuildScoutReportGridMatrix();
            for (int i = 0; i < CurrentScoutReportGridMatrix.Count(); i++) {
                List<ScoutReport>[] row = CurrentScoutReportGridMatrix[i];
                for (int j = 0; j < row.Count(); j++) {
                    List<ScoutReport> entry = CurrentScoutReportGridMatrix[i][j];
                    ClonedScoutReportGridMatrix[i][j] = CloneList(entry);
                }
            }
            return ClonedScoutReportGridMatrix;
        }

        private List<ScoutReport> CloneList(List<ScoutReport> reports)
        {
            if (reports == null) {
                return null;
            }

            List<ScoutReport> clone = new List<ScoutReport>();
            foreach (ScoutReport report in reports)
            {
                clone.Add(report);
            }
            return clone;
        }
    }

    public interface UpdateListener
    {
        void OnGridUpdated(List<ScoutReport>[][] ScoutReportGridMatrix, AggregateScoutReportData bestCell);
    }
}
