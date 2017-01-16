using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SQLite;

namespace OpenRA.Mods.Common.AI.Esu.Database
{
    public class UnitDamageDataTable
    {
        private const string UnitDamageDataTableName = "UnitDamageData";

        public static Column AttackingUnit = new Column("AttackingUnit", "TEXT");
        public static Column DamagedUnit = new Column("DamagedUnit", "TEXT");

        public static Column Damage = new Column("Damage", "INT");
        public static Column WasUnitKilled = new Column("WasUnitKilled", "INT");

        public static Column[] Columns = {
            AttackingUnit, DamagedUnit, Damage, WasUnitKilled
        };

        public UnitDamageDataTable()
        {
            CreateTableIfNotExists();
        }

        private void CreateTableIfNotExists()
        {
            SQLiteConnection connection = SQLiteConnectionUtils.GetDatabaseConnection();
            connection.Open();
            try
            {
                string createTable = SQLiteUtils.GetCreateTableIfNotExistsSQLCommandString(UnitDamageDataTableName, Columns);
                SQLiteCommand createTableCommand = new SQLiteCommand(createTable, connection);
                createTableCommand.ExecuteNonQuery();
            }
            finally
            {
                connection.Close();
            }
        }
    }
}
