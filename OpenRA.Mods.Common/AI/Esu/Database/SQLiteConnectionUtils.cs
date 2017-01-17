using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SQLite;

namespace OpenRA.Mods.Common.AI.Esu.Database
{
    static class SQLiteConnectionUtils
    {
        private const string DataSourceConnectionPrepend = "URI = file:";
        private const string DatabaseFileName = "EsuAIInformation.sqlite";

        public static SQLiteConnection GetDatabaseConnection()
        {
            string fileLocation = Platform.GetSupportDir() + DatabaseFileName;
            CreateFileIfNotExists(fileLocation);

            return new SQLiteConnection(DataSourceConnectionPrepend + fileLocation);
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
