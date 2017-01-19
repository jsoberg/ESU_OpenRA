using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRA.Mods.Common.AI.Esu.Database;
using System.Threading;
using System.Data.SQLite;

namespace OpenRA.Mods.Common.AI.Esu.Rules.Units
{
    public class CompiledUnitDamageStatisticsLoader
    {
        private CompiledUnitDamageStatistics UnitDamageStats;
        private readonly object StatsLock = new object();

        private readonly UnitDamageDataTable UnitDamageDataTable;
        
        public CompiledUnitDamageStatisticsLoader()
        {
            this.UnitDamageDataTable = new UnitDamageDataTable();
        }

        public void ReloadUnitDamageStats()
        {
            var thread = new Thread(() => LoadUnitDamageStats());
            thread.Start();
        }

        private void LoadUnitDamageStats()
        {
            SQLiteConnection connection = SQLiteConnectionUtils.GetDatabaseConnection();
            connection.Open();
            SQLiteDataReader reader = UnitDamageDataTable.Query(connection);

            try
            {
                LoadNewUnitDamageDataFromReader(reader);
            }
            finally
            {
                reader.Close();
                connection.Close();
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
    }
}