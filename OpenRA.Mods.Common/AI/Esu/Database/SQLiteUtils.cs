using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRA.Mods.Common.AI.Esu.Database
{
    public static class SQLiteUtils
    {
        public static string GetCreateTableIfNotExistsSQLCommandString(string tableName, Column[] columns)
        {
            string sql = "CREATE TABLE IF NOT EXISTS " + tableName + " ( ";
            int i = 0;
            foreach (Column col in columns) {
                i++;
                sql += col.ColumnName + " " + col.ColumnType;
                if (i < columns.Count()) {
                    sql += ", ";
                }
            }
            sql += ")";

            return sql;
        }

        public static string GetInsertSQLCommandString(string tableName, ColumnWithValue[] columnsAndValues)
        {
            string sql = "INSERT INTO " + tableName + " (";

            int i = 0;
            foreach (ColumnWithValue col in columnsAndValues) {
                i++;
                sql += col.Column.ColumnName;
                if (i < columnsAndValues.Count()) {
                    sql += ", ";
                }
            }
            sql += ") values (";

            i = 0;
            foreach (ColumnWithValue col in columnsAndValues) {
                i++;
                sql += col.Value.ToString();
                if (i < columnsAndValues.Count()) {
                    sql += ", ";
                }
            }
            sql += ")";

            return sql;
        }
    }

    public class Column
    {
        public readonly string ColumnName;
        public readonly string ColumnType;

        public Column(string colName, string colType)
        {
            this.ColumnName = colName;
            this.ColumnType = colType;
        }
    }

    public class ColumnWithValue
    {
        public readonly Column Column;
        public readonly Object Value;

        public ColumnWithValue(Column column, Object value)
        {
            this.Column = column;
            this.Value = value;
        }
    }
}
