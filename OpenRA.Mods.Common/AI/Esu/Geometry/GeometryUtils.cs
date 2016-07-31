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
    }
}
