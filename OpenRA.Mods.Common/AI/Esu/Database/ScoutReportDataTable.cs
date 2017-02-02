using System.Data.SQLite;
using OpenRA.Mods.Common.AI.Esu.Strategy.Scouting;

namespace OpenRA.Mods.Common.AI.Esu.Database
{
    public class ScoutReportDataTable
    {
        private const string ScoutReportDataTableName = "ScoutReportData";

        public static Column LowestRisk = new Column("LowestRisk", "INT");
        public static Column HighestRisk = new Column("HighestRisk", "INT");

        public static Column LowestReward = new Column("LowestReward", "INT");
        public static Column HighestReward = new Column("HighestReward", "INT");

        public static Column[] Columns = {
            LowestRisk, HighestRisk, LowestReward, HighestReward
        };

        public ScoutReportDataTable()
        {
            CreateTableIfNotExists();
        }

        private void CreateTableIfNotExists()
        {
            SQLiteConnection connection = SQLiteConnectionUtils.GetDatabaseConnection();
            if (connection == null) {
                return;
            }

            try
            {
                connection.Open();
                string createTable = SQLiteUtils.GetCreateTableIfNotExistsSQLCommandString(ScoutReportDataTableName, Columns);
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

        public void InsertScoutReportData(BestScoutReportData data)
        {
            SQLiteConnection connection = SQLiteConnectionUtils.GetDatabaseConnection();
            if (connection == null) {
                return;
            }

            try
            {
                connection.Open();

                ColumnWithValue[] colsWithValues = {
                    new ColumnWithValue(LowestRisk, data.LowestRisk),
                    new ColumnWithValue(HighestRisk, data.HighestRisk),
                    new ColumnWithValue(LowestReward, data.LowestReward),
                    new ColumnWithValue(HighestReward, data.HighestReward)

                };

                string insert = SQLiteUtils.GetInsertSQLCommandString(ScoutReportDataTableName, colsWithValues);
                SQLiteCommand insertCommand = new SQLiteCommand(insert, connection);
                insertCommand.ExecuteNonQuery();
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

        public BestScoutReportData QueryForBestScoutReportData()
        {
            SQLiteConnection connection = SQLiteConnectionUtils.GetDatabaseConnection();
            if (connection == null) {
                return null;
            }

            try
            {
                connection.Open();
                long count = SQLiteUtils.GetCountForTable(connection, ScoutReportDataTableName);
                if (count <= 0) {
                    return null;
                }

                BestScoutReportData.Builder builder = new BestScoutReportData.Builder()
                    .addRiskValue(QueryFirstValueOrderedByColumn(connection, LowestRisk, "ASC"))
                    .addRiskValue(QueryFirstValueOrderedByColumn(connection, HighestRisk, "DESC"))
                    .addRewardValue(QueryFirstValueOrderedByColumn(connection, LowestReward, "ASC"))
                    .addRewardValue(QueryFirstValueOrderedByColumn(connection, HighestReward, "DESC"));
                return builder.Build();
            }
            catch (SQLiteException)
            {
                SQLiteConnectionUtils.LogSqliteException();
                return null;
            }
            finally
            {
                connection.Close();
            }
        }

        private int QueryFirstValueOrderedByColumn(SQLiteConnection openConnection, Column column, string ascOrDesc)
        {
            string sql = "SELECT " + column.ColumnName + " FROM " + ScoutReportDataTableName + " ORDER BY " + column.ColumnName + " " + ascOrDesc;
            SQLiteCommand queryCommand = new SQLiteCommand(sql, openConnection);
            SQLiteDataReader reader = queryCommand.ExecuteReader();

            if (reader.Read()) {
                return (int) reader[column.ColumnName];
            }
            return int.MinValue;
        }
    }
}
