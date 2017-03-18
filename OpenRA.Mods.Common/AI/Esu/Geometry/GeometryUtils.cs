using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace OpenRA.Mods.Common.AI.Esu.Geometry
{
    class GeometryUtils
    {
        public static CPos GetPositionClosestToStart(CPos startPosition,  IEnumerable<CPos> positions)
        {
            CPos closestPosition = CPos.Invalid;
            double minDistance = double.MaxValue;

            foreach (CPos pos in positions) {
                double distance = EuclideanDistance(startPosition, pos);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestPosition = pos;
                }
            }
            return closestPosition;
        }

        public static double EuclideanDistance(CPos p1, CPos p2)
        {
            return Math.Sqrt(Math.Pow(p1.X - p2.X, 2) + Math.Pow(p1.Y - p2.Y, 2));
        }

        public static CPos OppositeLocationOnMap(CPos location, Map map)
        {
            var x = (map.Bounds.Right - (location.X - map.Bounds.Left));
            var y = (map.Bounds.Bottom - (location.Y - map.Bounds.Top));
            return new CPos(x, y);
        }

        public static CPos ParallelXLocationOnMap(CPos location, Map map)
        {
            var x = (map.Bounds.Right - (location.X - map.Bounds.Left));
            var y = location.Y;
            return new CPos(x, y);
        }

        public static double BearingBetween(CPos first, CPos second)
        {
            int deltaX = second.X - first.X;
            int deltaY = second.Y - first.Y;
            return Math.Atan2(deltaY, deltaX);
        }

        public static CPos MoveTowards(CPos start, CPos end, int distance, Map map)
        {
            double bearing = BearingBetween(start, end);
            return MoveTowards(start, bearing, distance, map);
        }

        public static CPos MoveTowards(CPos start, double bearing, int distance, Map map)
        {
            int changeX = (int)(distance * Math.Cos(bearing));
            int changeY = (int)(distance * Math.Sin(bearing));

            int towardX = SanitizedValue(start.X + changeX, map.Bounds.Left, map.Bounds.Right);
            int towardY = SanitizedValue(start.Y + changeY, map.Bounds.Top, map.Bounds.Bottom);
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
            var topLeft = new CPos(map.Bounds.Left, map.Bounds.Top);
            var topRight = new CPos(map.Bounds.Right, map.Bounds.Top);
            var botLeft = new CPos(map.Bounds.Left, map.Bounds.Bottom);
            var botRight = new CPos(map.Bounds.Right, map.Bounds.Bottom);

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
