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
            connection.Open();
            try {
                string createTable = SQLiteUtils.GetCreateTableIfNotExistsSQLCommandString(ScoutReportDataTableName, Columns);
                SQLiteCommand createTableCommand = new SQLiteCommand(createTable, connection);
                createTableCommand.ExecuteNonQuery();
            } finally {
                connection.Close();
            }
        }

        public void InsertScoutReportData(BestScoutReportData data)
        {
            SQLiteConnection connection = SQLiteConnectionUtils.GetDatabaseConnection();
            connection.Open();
            try {
                ColumnWithValue[] colsWithValues = {
                    new ColumnWithValue(LowestRisk, data.LowestRisk),
                    new ColumnWithValue(HighestRisk, data.HighestRisk),
                    new ColumnWithValue(LowestReward, data.LowestReward),
                    new ColumnWithValue(HighestReward, data.HighestReward)

                };

                string insert = SQLiteUtils.GetInsertSQLCommandString(ScoutReportDataTableName, colsWithValues);
                SQLiteCommand insertCommand = new SQLiteCommand(insert, connection);
                insertCommand.ExecuteNonQuery();
            } finally {
                connection.Close();
            }
        }
    }
}
