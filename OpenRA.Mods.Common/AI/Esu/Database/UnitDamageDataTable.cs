using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRA.Traits;
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
            SQLiteUtils.CreateTableIfNotExists(UnitDamageDataTableName, Columns);
        }

        public void InsertUnitDamageData(UnitDamageData data)
        {
            using (SQLiteConnection connection = SQLiteConnectionUtils.GetDatabaseConnection())
            {
                if (connection == null) {
                    return;
                }

                try
                {
                    ColumnWithValue[] colsWithValues = {
                        new ColumnWithValue(AttackingUnit, data.AttackerName),
                        new ColumnWithValue(DamagedUnit, data.DamagedUnitName),
                        new ColumnWithValue(Damage, data.Damage),
                        new ColumnWithValue(WasUnitKilled, data.WasKilled ? 1 : 0) };

                    string insert = SQLiteUtils.GetInsertSQLCommandString(UnitDamageDataTableName, colsWithValues);
                    using (SQLiteCommand insertCommand = new SQLiteCommand(insert, connection)) {
                        insertCommand.ExecuteNonQuery();
                    }
                }
                catch (SQLiteException e)
                {
                    SQLiteConnectionUtils.LogSqliteException(e);
                    return;
                }
            }
        }

        public SQLiteDataReader Query(SQLiteConnection connection)
        {
            string sql = "SELECT * FROM " + UnitDamageDataTableName;
            using (SQLiteCommand queryCommand = new SQLiteCommand(sql, connection)) {
                return queryCommand.ExecuteReader();
            }
        }
    }
}
