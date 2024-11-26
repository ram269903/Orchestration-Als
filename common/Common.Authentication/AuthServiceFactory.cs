using Common.Config;
using Microsoft.Extensions.Logging;

namespace Common.Authentication
{
    public class AuthServiceFactory
    {
        public static IAuthService GetAuthService(DbConfig dbConfig, AppSettings appSettings, ILoggerFactory loggerFactory)
        {

            ILogger _logger = loggerFactory.CreateLogger("AuthServiceFactory");

            switch (dbConfig.DataProvider.ToLower().Trim())
            {
                case "sqlserver": return new MsSql.AuthService(dbConfig, appSettings, _logger);
                case "mongodb": return new Mongo.AuthService(dbConfig, appSettings);
                case "postgres": return new PgSql.AuthService(dbConfig, appSettings);

                default: return new Mongo.AuthService(dbConfig, appSettings);
            }
        }

    }
}
