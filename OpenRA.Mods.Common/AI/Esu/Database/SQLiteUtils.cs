using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRA.Mods.Common.AI.Esu.Database
{
    public static class SQLiteUtils
    {
        public static string GetCreateTableIfNotExistsSQLCommand(string tableName, Column[] columns)
        {
            string sql = "CREATE TABLE IF NOT EXISTS " + tableName + " ( ";
            bool deleteFinalComma = false;
            foreach (Column col in columns)
            {
                sql += col.ColumnName + " " + col.ColumnType + ", ";
                deleteFinalComma = true;
            }
            if (deleteFinalComma) {
                sql = sql.Substring(0, sql.Length - 2);
            }
            sql += ")";

            return sql;
        }
    }

    public class Column
    {
        public string ColumnName;
        public string ColumnType;

        public Column(string colName, string colType)
        {
            this.ColumnName = colName;
            this.ColumnType = colType;
        }
    }
}
