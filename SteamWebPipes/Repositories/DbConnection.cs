using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteamCrawlerCore.Repositories
{
    public static class DbConnection
    {
        private static readonly string connectionString;
        public static void Initialize(string connectionString)
        {
            _ = connectionString;
        }

        public static IDbConnection CreateConnection()
        {
            if(string.IsNullOrEmpty(connectionString) || string.IsNullOrEmpty(connectionString.Trim()))
            {
                throw new ArgumentException("Connection string is null");
            }
            return new MySqlConnection(connectionString);
        }
    }
}
