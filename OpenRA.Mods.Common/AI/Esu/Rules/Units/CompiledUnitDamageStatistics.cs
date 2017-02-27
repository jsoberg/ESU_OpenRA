using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRA.Mods.Common.AI.Esu.Rules.Units
{
    public class CompiledUnitDamageStatistics
    {
        public readonly Dictionary<string, DamageKillStats> UnitNameToDamageStatsMap;

        public CompiledUnitDamageStatistics()
        {
            this.UnitNameToDamageStatsMap = new Dictionary<string, DamageKillStats>();
        }

        public void AddStatsForUnit(string attackingUnit, int damage, bool wasKill)
        {
            DamageKillStats stats = UnitNameToDamageStatsMap.ContainsKey(attackingUnit) ? 
                UnitNameToDamageStatsMap[attackingUnit] : new DamageKillStats();
            stats.Damage += damage;
            stats.NumEntries++;
            stats.KillCount += wasKill ? 1 : 0;

            UnitNameToDamageStatsMap[attackingUnit] = stats;
        }

        public Dictionary<string, DamageKillStats> GetStatsForActors(string[] actors)
        {
            Dictionary<string, DamageKillStats> actorStats = new Dictionary<string, DamageKillStats>();
            foreach (string name in actors)
            {
                if (UnitNameToDamageStatsMap.ContainsKey(name)) {
                    actorStats.Add(name, UnitNameToDamageStatsMap[name]);
                }
            }

            return actorStats;
        }
    }

    public class DamageKillStats
    {
        public int Damage;
        public int NumEntries;
        public int KillCount;

        public double DamagePerEntry()
        {
            return (double) Damage / (double) NumEntries;
        }
    }
}
