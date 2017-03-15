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
            catch (SQLiteException e)
            {
                LogSqliteException(e);
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
            builder.DefaultTimeout = 10000;

            return builder.ConnectionString;
        }

        private static void CreateFileIfNotExists(string fileLocation)
        {
            if (!System.IO.File.Exists(fileLocation))
            {
                SQLiteConnection.CreateFile(fileLocation);
            }
        }

        public static void LogSqliteException(Exception e)
        {
            Log.AddChannel("sqlite_errors", "sqlite_errors.log");
            Log.Write("sqlite_errors", "Problem opening SQLite connection: ");

            if (e != null) {
                StringBuilder exceptionReport = BuildExceptionReport(e);
                Log.Write("sqlite_errors", exceptionReport.ToString());
            }
        }

        static StringBuilder BuildExceptionReport(Exception e)
        {
            return BuildExceptionReport(e, new StringBuilder(), 0);
        }

        static StringBuilder BuildExceptionReport(Exception e, StringBuilder sb, int d)
        {
            if (e == null)
                return sb;

            sb.AppendFormat("Exception of type `{0}`: {1}", e.GetType().FullName, e.Message);
      
            if (e.InnerException != null)
            {
                sb.AppendLine();
                Indent(sb, d); sb.Append("Inner ");
                BuildExceptionReport(e.InnerException, sb, d + 1);
            }

            sb.AppendLine();
            Indent(sb, d); sb.Append(e.StackTrace);

            return sb;
        }

        static void Indent(StringBuilder sb, int d)
        {
            sb.Append(new string(' ', d * 2));
        }
    }
}
