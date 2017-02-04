using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SQLite;

namespace OpenRA.Mods.Common.AI.Esu.Database
{
    static class SQLiteConnectionUtils
    {
        private const string DatabaseFileName = "EsuAIInformation.sqlite";

        public static SQLiteConnection GetDatabaseConnection()
        {
            try
            {
                string fileLocation = Platform.GetSupportDir() + DatabaseFileName;
                CreateFileIfNotExists(fileLocation);

                string connectionString = BuildSQLiteConnectionString(fileLocation);
                SQLiteConnection connection = new SQLiteConnection(connectionString);
                connection.Open();
                return connection;
            }
            catch (SQLiteException)
            {
                LogSqliteException();
                return null;
            }
        }

        private static string BuildSQLiteConnectionString(string fileLocation)
        {
            SQLiteConnectionStringBuilder builder = new SQLiteConnectionStringBuilder();
            builder.DataSource = fileLocation;
            builder.Pooling = true;
            builder.SyncMode = SynchronizationModes.Full;
            builder.FailIfMissing = false;

            return builder.ConnectionString;
        }

        private static void CreateFileIfNotExists(string fileLocation)
        {
            if (!System.IO.File.Exists(fileLocation))
            {
                SQLiteConnection.CreateFile(fileLocation);
            }
        }

        public static void LogSqliteException()
        {
            Log.AddChannel("sqlite_errors", "sqlite_errors.log");
            Log.Write("sqlite_errors", "Problem opening SQLite connection ");
        }
    }
}
