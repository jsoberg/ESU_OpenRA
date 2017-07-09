using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRA.Mods.Common.Traits;
using System.Data.SQLite;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.AI.Esu.Database
{
	class FundBalanceTable
	{
		private const string FundBalanceTableName = "FundBalance";

		public static Column PlayerName = new Column("PlayerName", "TEXT");
		public static Column TickCount = new Column("TickCount", "INT");
		public static Column ResourcesOnHand = new Column("ResourcesOnHand", "INT");

		public static Column[] Columns = {
			PlayerName, TickCount, ResourcesOnHand
		};

		public FundBalanceTable()
		{
			SQLiteUtils.CreateTableIfNotExists(FundBalanceTableName, Columns);
		}

		public void InsertFundData(Player player, World world)
		{
			using (SQLiteConnection connection = SQLiteConnectionUtils.GetDatabaseConnection())
			{
				if (connection == null)
				{
					return;
				}

				try
				{
					int tickCount = world.GetCurrentLocalTickCount();
					var res = player.PlayerActor.Trait<PlayerResources>();
					int fundsOnHand = res.DisplayCash + res.DisplayResources;

					ColumnWithValue[] colsWithValues = {
						new ColumnWithValue(PlayerName, "\"" + player.PlayerName + "\""),
						new ColumnWithValue(TickCount, tickCount),
						new ColumnWithValue(ResourcesOnHand, fundsOnHand)
					};

					string insert = SQLiteUtils.GetInsertSQLCommandString(FundBalanceTableName, colsWithValues);
					using (SQLiteCommand insertCommand = new SQLiteCommand(insert, connection))
					{
						insertCommand.ExecuteNonQuery();
					}
				}
				catch (SQLiteException e)
				{
					SQLiteConnectionUtils.LogSqliteException(e);
					return;
				}
				connection.Close();
			}
		}
	}
}
