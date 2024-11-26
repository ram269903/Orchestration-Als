using Common.DataAccess.RDBMS;
using Common.DataAccess.RDBMS.Model;
using Npgsql;

namespace Common.DataAccess.PostgreSql
{
    public class DbHelper : IDbHelper
    {
        public bool CheckConnection(DbDatabase database)
        {
            var connectionString = GetConnectionString(database);
            return CheckConnection(connectionString);
        }

        public bool CheckConnection(string connectionString)
        {
            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();
                return true;
            }
        }

        public string GetConnectionString(DbDatabase database)
        {
            //string connString;

            //if (database.Port == null)
            //    connString = $"Server={database.Server};Database={database.DatabaseName};User Id={database.UserId};Password={database.Password};";
            //else
            //    connString = $"Server={database.Server},{database.Port};Database={database.DatabaseName};User Id={database.UserId};Password={database.Password};";

            //return connString;

            var connStringBuilder = new NpgsqlConnectionStringBuilder
            {
                Host = database.Server,
                Port = (database.Port == 0 || database.Port == null) ? 5432 : (int)database.Port,
                Database = database.DatabaseName,
                Username = database.UserId,
                Password = database.Password
            };

            return connStringBuilder.ConnectionString;
        }

        public DbDatabase GetDatabase(string connectionString)
        {
            var builder = new NpgsqlConnectionStringBuilder(connectionString);

            return new DbDatabase
            {
                Server = builder.Host,
                DatabaseName = builder.Database,
            };
        }

        public string GetDatabaseName(string connectionString)
        {
            var builder = new NpgsqlConnectionStringBuilder(connectionString);

            return builder.Database;
        }

        public string GetServerName(string connectionString)
        {
            var builder = new NpgsqlConnectionStringBuilder(connectionString);

            return builder.Host;
        }
    }
}
