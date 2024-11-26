using Common.DataAccess.RDBMS;
using Common.DataAccess.RDBMS.Model;
using System.Net;
using MySql.Data.MySqlClient;

namespace Common.DataAccess.MySql
{
    public class DbHelper : IDbHelper
    {
        public bool CheckConnection(string connectionString)
        {
            using (var connection = new MySqlConnection(connectionString))
            {
                connection.Open();
                return true;
            }
        }

        public bool CheckConnection(DbDatabase database)
        {
            var connectionString = GetConnectionString(database);
            return CheckConnection(connectionString);
        }

        public string GetConnectionString(DbDatabase database)
        {
            //string connString;

            //if (database.Port == 0 || database.Port == null)
            //    database.Port = 3306;

            //connString = $"server={database.Server};port={database.Port};database={database.DatabaseName};uid={database.UserId};pwd={database.Password};";

            //return connString;


            var connStringBuilder = new MySqlConnectionStringBuilder
            {
                Server = database.Server,
                Port = (database.Port == 0 || database.Port == null) ? 3306 : (uint)database.Port,
                Database = database.DatabaseName,
                UserID = database.UserId,
                Password = database.Password
            };

            return connStringBuilder.ConnectionString;
        }

        public DbDatabase GetDatabase(string connectionString)
        {
            var builder = new MySqlConnectionStringBuilder(connectionString);

            return new DbDatabase
            {
                Server = builder.Server,
                DatabaseName = builder.Database,
            };
        }

        public string GetDatabaseName(string connectionString)
        {
            var builder = new MySqlConnectionStringBuilder(connectionString);

            return builder.Database;
        }

        public string GetServerName(string connectionString)
        {
            var builder = new MySqlConnectionStringBuilder(connectionString);

            return builder.Server;
        }
    }
}
