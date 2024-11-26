using Common.DataAccess.RDBMS.Model;

namespace Common.DataAccess.RDBMS
{
    public interface IDbHelper
    {
        bool CheckConnection(DbDatabase database);

        bool CheckConnection(string connectionString);

        string GetConnectionString(DbDatabase database);

        DbDatabase GetDatabase(string connectionString);

        string GetDatabaseName(string connectionString);

        string GetServerName(string connectionString);

    }
}
