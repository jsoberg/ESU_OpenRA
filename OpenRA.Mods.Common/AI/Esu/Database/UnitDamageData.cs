using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.AI.Esu.Database
{
    public class UnitDamageData
    {
        public readonly string AttackerName;
        public readonly string DamagedUnitName;
        public readonly int Damage;
        public readonly bool WasKilled;

        public UnitDamageData(Actor damagedUnit, AttackInfo attackInfo)
        {
            this.AttackerName = "\"" + attackInfo.Attacker.Info.Name + "\"";
            this.DamagedUnitName = "\"" + damagedUnit.Info.Name + "\"";
            this.Damage = attackInfo.Damage;
            this.WasKilled = damagedUnit.IsDead;
        }
    }
}
