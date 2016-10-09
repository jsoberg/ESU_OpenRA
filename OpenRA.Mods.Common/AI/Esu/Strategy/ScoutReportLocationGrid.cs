using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRA.Mods.Common.AI.Esu.Strategy
{
    public class ScoutReportLocationGrid
    {
        private const double WIDTH_PER_GRID_SQUARE = 10;

        private readonly List<ScoutReport>[][] ScoutReportGridMatrix;

        public ScoutReportLocationGrid(World world)
        {
            ScoutReportGridMatrix = BuildScoutReportGridMatrix(world);
        }

        private List<ScoutReport>[][] BuildScoutReportGridMatrix(World world)
        {
            int gridWidth = (int) Math.Round((double)world.Map.MapSize.X / WIDTH_PER_GRID_SQUARE);
            int gridHeight = (int) Math.Round((double)world.Map.MapSize.Y / WIDTH_PER_GRID_SQUARE);

            List<ScoutReport>[][] grid = new List<ScoutReport>[gridWidth][];
            for (int i = 0; i < gridWidth; i ++) {
                grid[i] = new List<ScoutReport>[gridHeight];
            }
            return grid;
        }
    }
}
