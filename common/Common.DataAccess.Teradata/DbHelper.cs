using Common.DataAccess.RDBMS;
using Common.DataAccess.RDBMS.Model;
using Teradata.Client.Provider;

namespace Common.DataAccess.Teradata
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
            using (var connection = new TdConnection(connectionString))
            {
                connection.Open();
                return true;
            }
        }

        public string GetConnectionString(DbDatabase database)
        {
            //string connString;

            //if (database.Port == null)
            //    connString = $"Data Source={database.Server};User ID={database.UserId};Password={database.Password};";
            //else
            //    connString = $"Data Source={database.Server}:{database.Port};Database={database.DatabaseName};User ID={database.UserId};Password={database.Password};";

            //return connString;

            var connStringBuilder = new TdConnectionStringBuilder
            {
                DataSource = database.Server,
                PortNumber = (database.Port == 0 || database.Port == null) ? 1025 : (int)database.Port,
                Database = database.DatabaseName,
                UserId = database.UserId,
                Password = database.Password
            };

            return connStringBuilder.ConnectionString;
        }

        public DbDatabase GetDatabase(string connectionString)
        {
            var builder = new TdConnectionStringBuilder(connectionString);

            return new DbDatabase
            {
                Server = builder.DataSource,
                DatabaseName = builder.Database,
            };
        }

        public string GetDatabaseName(string connectionString)
        {
            var builder = new TdConnectionStringBuilder(connectionString);

            return builder.Database;
        }

        public string GetServerName(string connectionString)
        {
            var builder = new TdConnectionStringBuilder(connectionString);

            return builder.DataSource;
        }
    }
}
