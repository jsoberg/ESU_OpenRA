using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRA.Support;
using OpenRA.Mods.Common.AI.Esu.Database;
using System.Threading;
using System.Data.SQLite;

namespace OpenRA.Mods.Common.AI.Esu.Rules.Units
{
    public class CompiledUnitDamageStatisticsLoader
    {
        private static MersenneTwister RANDOM = new MersenneTwister();

        private CompiledUnitDamageStatistics UnitDamageStats;
        private readonly object StatsLock = new object();

        private readonly UnitDamageDataTable UnitDamageDataTable;
        
        public CompiledUnitDamageStatisticsLoader()
        {
            this.UnitDamageDataTable = new UnitDamageDataTable();
            ReloadUnitDamageStats();
        }

        public CompiledUnitDamageStatistics GetUnitDamageStatistics()
        {
            lock (StatsLock) {
                return UnitDamageStats;
            }
        }

        public Dictionary<string, DamageKillStats> GetStatsForActors(string[] actors)
        {
            CompiledUnitDamageStatistics stats;
            lock (StatsLock) {
                stats = UnitDamageStats;
            }

            if (stats != null) {
                return stats.GetStatsForActors(actors);
            }
            return null;
        }

        public void ReloadUnitDamageStats()
        {
            ThreadPool.QueueUserWorkItem(t => LoadUnitDamageStats());
        }

        private void LoadUnitDamageStats()
        {
            using (SQLiteConnection connection = SQLiteConnectionUtils.GetDatabaseConnection())
            {
                if (connection == null) {
                    return;
                }

                try
                {
                    using (SQLiteDataReader reader = UnitDamageDataTable.Query(connection)) {
                        LoadNewUnitDamageDataFromReader(reader);
                    }
                }
                catch (SQLiteException e)
                {
                    SQLiteConnectionUtils.LogSqliteException(e);
                    return;
                }
            }
        }

        private void LoadNewUnitDamageDataFromReader(SQLiteDataReader reader)
        {
            CompiledUnitDamageStatistics stats = new CompiledUnitDamageStatistics();
            while (reader.Read())
            {
                string attackerName = (string)reader[UnitDamageDataTable.AttackingUnit.ColumnName];
                int damage = (int)reader[UnitDamageDataTable.Damage.ColumnName];
                bool wasKilled = ((int)reader[UnitDamageDataTable.WasUnitKilled.ColumnName]) > 0;

                stats.AddStatsForUnit(attackerName, damage, wasKilled);
            }

            lock (StatsLock)
            {
                UnitDamageStats = stats;
            }
        }

        public string GetUnitForStats(Dictionary<string, DamageKillStats> stats)
        {
            double totalDamagePerEntry = 0;
            foreach (DamageKillStats stat in stats.Values)
            {
                totalDamagePerEntry += stat.DamagePerEntry();
            }

            Dictionary<float, string> percentDamageToUnit = new Dictionary<float, string>();
            foreach (KeyValuePair<string, DamageKillStats> stat in stats)
            {
                percentDamageToUnit.Add((float)(stat.Value.DamagePerEntry() / totalDamagePerEntry), stat.Key);
            }

            var sorted = from entry in percentDamageToUnit orderby entry.Value descending select entry;
            float val = RANDOM.NextFloat();
            float current = 0;
            foreach (KeyValuePair<float, string> entry in sorted)
            {
                current += entry.Key;
                if (val <= current)
                {
                    return entry.Value;
                }
            }
            return null;
        }
    }
}