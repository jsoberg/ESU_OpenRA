using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRA.Mods.Common.Traits;
using System.Data.SQLite;

namespace OpenRA.Mods.Common.AI.Esu.Database
{
    public class EndGameDataTable
    {
        private const string EndGameDataTableName = "EndGameData";

        public static Column PlayerName = new Column("PlayerName", "TEXT");
        public static Column KillCost = new Column("KillCost", "INT");
        public static Column DeathCost = new Column("DeathCost", "INT");
        public static Column UnitsKilled = new Column("UnitsKilled", "INT");
        public static Column UnitsDead = new Column("UnitsDead", "INT");
        public static Column BuildingsKilled = new Column("BuildingsKilled", "INT");
        public static Column BuildingsDead = new Column("BuildingsDead", "INT");
        public static Column GameTickCount = new Column("GameTickCount", "INT");

        // Columns added after initial table.
        public static Column Winner = new Column("Winner", "INT");

        public static Column[] OriginalColumns = {
            PlayerName, KillCost, DeathCost, UnitsKilled, UnitsDead, BuildingsKilled, BuildingsDead, GameTickCount
        };

        public EndGameDataTable()
        {
            SQLiteUtils.CreateTableIfNotExists(EndGameDataTableName, OriginalColumns);
            SQLiteUtils.AlterTableAddColumn(EndGameDataTableName, Winner);
        }

        public void InsertEndGameData(string playerName, string winnerName, PlayerStatistics stats, World world)
        {
            SQLiteConnection connection = SQLiteConnectionUtils.GetDatabaseConnection();
            if (connection == null)
            {
                return;
            }

            try
            {
                ColumnWithValue[] colsWithValues = {
                    new ColumnWithValue(PlayerName, "\"" + playerName + "\""),
                    new ColumnWithValue(KillCost, stats.KillsCost),
                    new ColumnWithValue(DeathCost, stats.DeathsCost),
                    new ColumnWithValue(UnitsKilled, stats.UnitsKilled),
                    new ColumnWithValue(UnitsDead, stats.UnitsDead),
                    new ColumnWithValue(BuildingsKilled, stats.BuildingsKilled),
                    new ColumnWithValue(BuildingsDead, stats.BuildingsDead),
                    new ColumnWithValue(GameTickCount, world.GetCurrentLocalTickCount()),
                    new ColumnWithValue(Winner, playerName == winnerName ? 1 : 0)
                };

                string insert = SQLiteUtils.GetInsertSQLCommandString(EndGameDataTableName, colsWithValues);
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
    }
}
