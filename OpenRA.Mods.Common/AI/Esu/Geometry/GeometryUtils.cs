using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRA.Mods.Common.AI.Esu.Geometry
{
    class GeometryUtils
    {
        public static double EuclideanDistance(CPos p1, CPos p2)
        {
            return Math.Sqrt(Math.Pow(p1.X - p2.X, 2) + Math.Pow(p1.Y - p2.Y, 2));
        }

        public static CPos OppositeLocationOnMap(CPos location, Map map)
        {
            var width = map.MapSize.X;
            var height = map.MapSize.Y;

            var x = width - location.X;
            var y = height - location.Y;

            return new CPos(x, y);
        }

        public static CPos MoveTowards(CPos start, CPos end, int distance, Map map)
        {
            int deltaX = start.X - end.X;
            int deltaY = start.Y - end.Y;
            double angle = Math.Atan2(deltaY, deltaX);

            int changeX = (int) (distance * Math.Cos(angle));
            int changeY = (int) (distance * Math.Sin(angle));

            int towardX = SanitizedValue(start.X + changeX, 0, map.MapSize.X);
            int towardY = SanitizedValue(start.Y + changeY, 0, map.MapSize.Y);
            return new CPos(towardX, towardY);
        }

        private static int SanitizedValue(int value, int min, int max)
        {
            int sanitizedValue = Math.Min(value, max);
            return Math.Max(sanitizedValue, min);
        }

        public static CPos OppositeCornerOfNearestCorner(Map map, CPos currentLoc)
        {
            var corners = GetMapCorners(map);

            // Opposite corner will be farthest away.
            int largestDistIndex = 0;
            double largestDist = double.MinValue;
            for (int i = 0; i < corners.Count(); i++)
            {
                double dist = GeometryUtils.EuclideanDistance(currentLoc, corners[i]);
                if (dist > largestDist)
                {
                    largestDistIndex = i;
                    largestDist = dist;
                }
            }

            return corners[largestDistIndex];
        }

        public static CPos[] GetMapCorners(Map map)
        {
            var width = map.MapSize.X;
            var height = map.MapSize.Y;

            var topLeft = new CPos(0, 0);
            var topRight = new CPos(width, 0);
            var botLeft = new CPos(0, height);
            var botRight = new CPos(width, height);

            return new CPos[] { topLeft, topRight, botLeft, botRight };
        }

        /** @return The center position of all specified positions. */
        public static CPos Center(IEnumerable<CPos> positions)
        {
            if (positions == null || positions.Count() == 0) {
                return CPos.Invalid;
            }

            int x = 0, y = 0;
            foreach (CPos pos in positions)
            {
                x += pos.X;
                y += pos.Y;
            }

            return new CPos(x / positions.Count(), y / positions.Count());
        }
    }
}
