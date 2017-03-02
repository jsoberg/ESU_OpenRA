using System;
using System.Collections.Generic;
using System.Linq;

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

        /** @return Rank for the given actors for the specified group, where 1 is the lowest (weakest) rank. */
        public int GetDamageModifierForActorSubset(string actor, string[] actors)
        {
            Dictionary<string, DamageKillStats> stats = GetStatsForActors(actors);
            // Don't include actors that have little to no data.
            var ordered = stats.Where(a => a.Value.NumEntries > 10).OrderByDescending(a => a.Value);

            int rank = stats.Count();
            foreach (KeyValuePair<string, DamageKillStats> entry in ordered) {
                if (entry.Key == actor) {
                    return rank;
                }
                rank--;
            }
            // Default rank is 1.
            return 1;
        }
    }

    public class DamageKillStats : System.IComparable<DamageKillStats>
    {
        public int Damage;
        public int NumEntries;
        public int KillCount;

        public double DamagePerEntry()
        {
            return (double) Damage / (double) NumEntries;
        }

        int IComparable<DamageKillStats>.CompareTo(DamageKillStats other)
        {
            return DamagePerEntry().CompareTo(other.DamagePerEntry());
        }
    }
}
