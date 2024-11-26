using Common.Config;

namespace Common.Licenses
{
    public class LicenseServiceFactory
    {
        public static ILicenseService GetLicenseService(DbConfig dbConfig)
        {
            switch (dbConfig.DataProvider.ToLower().Trim())
            {
                case "sqlserver": return new MsSql.LicenseService(dbConfig);
                case "mongodb": return new Mongo.LicenseService(dbConfig);
                case "postgres": return new PgSql.LicenseService(dbConfig);

                default: return new Mongo.LicenseService(dbConfig);
            }
        }
    }
}
