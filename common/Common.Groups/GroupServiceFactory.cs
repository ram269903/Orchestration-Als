using Common.Config;

namespace Common.Groups
{
    public class GroupServiceFactory
    {
        public static IGroupService GetGroupService(DbConfig dbConfig)
        {
            switch (dbConfig.DataProvider.ToLower().Trim())
            {
                case "sqlserver": return new MsSql.GroupService(dbConfig);
                case "mongodb": return new Mongo.GroupService(dbConfig);
                case "postgres": return new PgSql.GroupService(dbConfig);

                default: return new Mongo.GroupService(dbConfig);
            }
        }
    }
}
