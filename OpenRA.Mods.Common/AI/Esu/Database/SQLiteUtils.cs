using System;
using System.Linq;
using System.Data.SQLite;

namespace OpenRA.Mods.Common.AI.Esu.Database
{
    public static class SQLiteUtils
    {
        public static void CreateTableIfNotExists(string tableName, Column[] columns)
        {
            SQLiteConnection connection = SQLiteConnectionUtils.GetDatabaseConnection();
            if (connection == null)
            {
                return;
            }

            try
            {
                string createTable = SQLiteUtils.GetCreateTableIfNotExistsSQLCommandString(tableName, columns);
                SQLiteCommand createTableCommand = new SQLiteCommand(createTable, connection);
                createTableCommand.ExecuteNonQuery();
            }
            catch (SQLiteException)
            {
                SQLiteConnectionUtils.LogSqliteException();
                return;
            }
            finally
            {
                connection.Close();
            }
        }

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

        public static long GetCountForTable(SQLiteConnection openConnection, string tableName)
        {
            string sql = "SELECT COUNT(*) FROM " + tableName;
            SQLiteCommand countCommand = new SQLiteCommand(sql, openConnection);
            return (long) countCommand.ExecuteScalar();
        }

        public static void AlterTableAddColumn(string tableName, Column column)
        {
            SQLiteConnection connection = SQLiteConnectionUtils.GetDatabaseConnection();
            if (connection == null)
            {
                return;
            }

            try
            {
                string addColumn = SQLiteUtils.GetAddColumnToTableSQLCommandString(tableName, column);
                SQLiteCommand createTableCommand = new SQLiteCommand(addColumn, connection);
                createTableCommand.ExecuteNonQuery();
            }
            catch (SQLiteException)
            {
                SQLiteConnectionUtils.LogSqliteException();
                return;
            }
            finally
            {
                connection.Close();
            }
        }

        public static string GetAddColumnToTableSQLCommandString(string tableName, Column column)
        {
            return "ALTER TABLE " + tableName + " ADD COLUMN " + column.ColumnName + " " + column.ColumnType;
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
