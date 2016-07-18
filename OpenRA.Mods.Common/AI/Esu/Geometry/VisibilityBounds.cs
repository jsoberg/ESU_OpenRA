using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRA.Mods.Common.AI.Esu.Geometry
{
    public class VisibilityBounds
    {
        private readonly List<Rect> boundingRects;

        public VisibilityBounds()
        {
            this.boundingRects = new List<Rect>();
        }

        public void AddRect(Rect rect)
        {
            boundingRects.Add(rect);
        }

        public bool ContainsPosition(WPos position)
        {
            foreach (Rect rect in boundingRects) {
                if (rect.ContainsPosition(position)) {
                    return true;
                }
            }

            return false;
        }
    }
}
