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

        public string ExpectedUnit { get; internal set; } 

        public int UnitCount { get { return units.Count; } }
        private readonly List<Actor> units;

        public UnitGroup(Purpose purpose)
        {
            this.Purpose = purpose;
            this.units = new List<Actor>();
        }

        public void RemoveDeadUnits()
        {
            for (int i = (units.Count - 1); i >= 0; i--) {
                if (units[i].IsDead) {
                    units.RemoveAt(i);
                }
            }
        }

        public void ExpectUnit(string expected)
        {
            ExpectedUnit = expected;
        }

        public void StopExpectingUnit()
        {
            ExpectedUnit = null;
        }

        public void AddUnitToGroup(Actor unit)
        {
            units.Add(unit);
            ExpectedUnit = null;
        }
    }
}
