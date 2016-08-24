using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRA.Mods.Common.AI.Esu;

namespace OpenRA.Mods.Common.AI.Esu
{
    public enum Purpose {
        Offense,
        Defense
    };

    public class UnitGroup
    {
        public readonly Purpose Purpose;

        private readonly List<Actor> units;

        public UnitGroup(Purpose purpose)
        {
            this.Purpose = purpose;
            this.units = new List<Actor>();
        }
    }
}
