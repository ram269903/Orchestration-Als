using Common.Config;

namespace Common.ActivityLogs
{
    public class ActivityLogServiceFactory
    {
        public static IActivityLogService GetActivityLogService(DbConfig dbConfig)
        {
            switch (dbConfig.DataProvider.ToLower().Trim())
            {
                case "sqlserver": return new MsSql.ActivityLogService(dbConfig);
                case "mongodb": return new Mongo.ActivityLogService(dbConfig);
                case "postgres": return new PgSql.ActivityLogService(dbConfig);

                default: return new Mongo.ActivityLogService(dbConfig);
            }
        }
    }
}
