using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRA.Mods.Common.AI.Esu.Strategy
{
    public class ScoutReportLocationGrid
    {
        private const int WIDTH_PER_GRID_SQUARE = 10;

        private readonly List<ScoutReport>[][] ScoutReportGridMatrix;

        public ScoutReportLocationGrid(World world)
        {
            ScoutReportGridMatrix = BuildScoutReportGridMatrix(world);
        }

        private List<ScoutReport>[][] BuildScoutReportGridMatrix(World world)
        {
            int gridWidth = GetRoundedIntDividedByWidth(world.Map.MapSize.X);
            int gridHeight = GetRoundedIntDividedByWidth(world.Map.MapSize.Y);

            List<ScoutReport>[][] grid = new List<ScoutReport>[gridWidth][];
            for (int i = 0; i < gridWidth; i ++) {
                grid[i] = new List<ScoutReport>[gridHeight];
            }
            return grid;
        }

        public void AddScoutReportForActor(Actor actor, ScoutReport report)
        {
            WPos scoutPosition = actor.CenterPosition;
            int x = GetRoundedIntDividedByWidth(scoutPosition.X);
            int y = GetRoundedIntDividedByWidth(scoutPosition.Y);

            List<ScoutReport> reportsForLocation = ScoutReportGridMatrix[x][y];
            if (reportsForLocation == null) {
                reportsForLocation = new List<ScoutReport>();
                ScoutReportGridMatrix[x][y] = reportsForLocation;
            }
            reportsForLocation.Add(report);
        }

        private int GetRoundedIntDividedByWidth(int pos)
        {
            return (int) Math.Round((double)pos / (double)WIDTH_PER_GRID_SQUARE);
        }
    }
}
