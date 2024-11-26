using Common.DataAccess.RDBMS;
using Common.DataAccess.RDBMS.Model;
using System.Data.SqlClient;
using System.Net;

namespace Common.DataAccess.MsSql
{
    public class DbHelper : IDbHelper
    {
        public bool CheckConnection(string connectionString)
        {
            using (var connection = new SqlConnection(connectionString))
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
            string connString;

            bool validateIp = IPAddress.TryParse(database.Server, out IPAddress ip);

            if (validateIp)
            {
                if (database.Port == null)
                    connString = $"Data Source=tcp:{database.Server};Initial Catalog={database.DatabaseName};User ID={database.UserId};Password={database.Password};";
                else
                    connString = $"Data Source=tcp:{database.Server},{database.Port};Initial Catalog={database.DatabaseName};User ID={database.UserId};Password={database.Password};";
            }
            else
            {
                if (database.Port == null)
                    connString = $"Server={database.Server};Database={database.DatabaseName};User Id={database.UserId};Password={database.Password};";
                else
                    connString = $"Server={database.Server},{database.Port};Database={database.DatabaseName};User Id={database.UserId};Password={database.Password};";
            }

            return connString;


            //var connStringBuilder = new SqlConnectionStringBuilder
            //{
            //    InitialCatalog = database.DatabaseName,
            //    UserID = database.UserId,
            //    Password = database.Password
            //};

            //bool validateIp = IPAddress.TryParse(database.Server, out IPAddress ip);

            //var port = (database.Port == null) ? 1433 : (int)database.Port;

            //if (validateIp)
            //    connStringBuilder.DataSource = $"tcp:{database.Server},{port}";
            //else
            //    connStringBuilder.DataSource = $"{database.Server},{port}";

            //return connStringBuilder.ConnectionString;
        }

        public DbDatabase GetDatabase(string connectionString)
        {
            var builder = new SqlConnectionStringBuilder(connectionString);

            return new DbDatabase
            {
                Server = builder.DataSource,
                DatabaseName = builder.InitialCatalog,
            };
        }

        public string GetDatabaseName(string connectionString)
        {
            var builder = new SqlConnectionStringBuilder(connectionString);

            return builder.InitialCatalog;
        }

        public string GetServerName(string connectionString)
        {
            var builder = new SqlConnectionStringBuilder(connectionString);

            return builder.DataSource;
        }

        //------------------
        //public static string GetConnectionString(DbDatabase database)
        //{
        //    string connString;

        //    if (database.Port == 0)
        //        connString = string.Format("Server={0};Database={1};User Id={2};Password={3}", database.Server, database.DatabaseName, database.UserId, database.Password);
        //    else
        //        connString = string.Format("Server={0},{1};Database={2};User Id={3};Password={4}", database.Server, database.Port, database.DatabaseName, database.UserId, database.Password);

        //    return connString;
        //}

        //public static DbDatabase GetDatabase(string connectionString)
        //{
        //    var builder = new SqlConnectionStringBuilder(connectionString);

        //    return new DbDatabase
        //    {
        //        Server = builder.DataSource,
        //        DatabaseName = builder.InitialCatalog,
        //    };
        //}

        //public static bool CheckDatabaseConnection(DbDatabase database)
        //{
        //    var connectionString = GetConnectionString(database);
        //    return CheckDatabaseConnection(connectionString);
        //}

        //public static bool CheckDatabaseConnection(string connectionString)
        //{
        //    using (var connection = new SqlConnection(connectionString))
        //    {
        //        connection.Open();
        //        return true;
        //    }
        //}

        //public static List<string> GetTableNames(string connectionString)
        //{
        //    return TablesHelper.GetTableNames(connectionString);
        //}

        ////public static string[,] GetSchemaTableNames(string connectionString)
        ////{
        ////    return TablesHelper.GetSchemaTableNames(connectionString);
        ////}

        //public static List<DbTable> GetTablesDetails(string connectionString, List<string> tablesList = null)
        //{
        //    return TablesHelper.GetTablesDetails(connectionString, tablesList);
        //}

        //public static List<string> GetViewNames(string connectionString)
        //{
        //    return ViewsHelper.GetViewNames(connectionString);
        //}

        //public static List<DbView> GetViewsDetails(string connectionString, List<string> viewsList = null)
        //{
        //    return ViewsHelper.GetViewsDetails(connectionString, viewsList);
        //}

    }
}
