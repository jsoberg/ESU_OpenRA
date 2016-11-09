using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SQLite;

namespace OpenRA.Mods.Common.AI.Esu.Database
{
    public static class SQLiteConnectionUtils
    {
        private const string DatabaseFileName = "EsuAIInformation.sqlite";

        private static SQLiteConnection DatabaseConnection;

        static SQLiteConnection GetDatabaseConnection()
        {
            if (DatabaseConnection == null) {
                string fileLocation = Platform.GetSupportDir() + DatabaseFileName;
                CreateFileIfNotExists(fileLocation);

                DatabaseConnection = new SQLiteConnection(fileLocation);
            }
            return DatabaseConnection;
        }

        private static void CreateFileIfNotExists(string fileLocation)
        {
            if (!System.IO.File.Exists(fileLocation))
            {
                SQLiteConnection.CreateFile(fileLocation);
            }
        }
    }
}
