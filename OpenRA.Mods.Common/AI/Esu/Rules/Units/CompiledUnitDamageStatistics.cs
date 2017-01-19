using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRA.Mods.Common.AI.Esu.Rules.Units
{
    public class CompiledUnitDamageStatistics
    {
        private readonly Dictionary<string, DamageKillStats> UnitNameToDamageStatsMap;

        public CompiledUnitDamageStatistics()
        {
            this.UnitNameToDamageStatsMap = new Dictionary<string, DamageKillStats>();
        }

        public void AddStatsForUnit(string attackingUnit, int damage, bool wasKill)
        {
            DamageKillStats stats = UnitNameToDamageStatsMap.ContainsKey(attackingUnit) ? 
                UnitNameToDamageStatsMap[attackingUnit] : new DamageKillStats();
            stats.Damage += damage;
            stats.KillCount += wasKill ? 1 : 0;

            UnitNameToDamageStatsMap[attackingUnit] = stats;
        }
    }

    class DamageKillStats
    {
        public int Damage;
        public int KillCount;
    }
}
