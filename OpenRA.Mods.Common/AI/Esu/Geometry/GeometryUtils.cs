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
    }
}
