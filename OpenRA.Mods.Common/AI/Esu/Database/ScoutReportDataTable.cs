using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SQLite;

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
                string createTable = SQLiteUtils.GetCreateTableIfNotExistsSQLCommand(ScoutReportDataTableName, Columns);
                SQLiteCommand createTableCommand = new SQLiteCommand(createTable, connection);
                createTableCommand.ExecuteNonQuery();
            } finally {
                connection.Close();
            }
        }


    }
}
