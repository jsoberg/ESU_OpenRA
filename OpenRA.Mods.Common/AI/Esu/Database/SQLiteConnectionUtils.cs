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

        private static SQLiteConnection DatabaseConnection;

        static SQLiteConnection GetDatabaseConnection()
        {
            if (DatabaseConnection == null) {
                DatabaseConnection =  new SQLiteConnection(DatabaseFileName);
            }
            return DatabaseConnection;
        }
    }
}
