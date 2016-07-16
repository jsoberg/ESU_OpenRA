using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRA.Mods.Common.AI.Esu.Geometry
{ 
    public class Rect
    {
        public readonly int Left;
        public readonly int Right;
        public readonly int Top;
        public readonly int Bottom;

        public Rect(int left, int right, int top, int bottom)
        {
            this.Left = left;
            this.Right = right;
            this.Top = top;
            this.Bottom = bottom;
        }

        public Rect(WPos position, int range)
        {
            this.Left = position.X - range;
            this.Right = position.X + range;
            this.Top = position.Y + range;
            this.Bottom = position.Y - range;
        }

        public bool ContainsPosition(WPos pos)
        {
            return (pos.X < Right && pos.X > Left && pos.Y < Top && pos.Y > Bottom);
        }
    }
}
