using Common.Config;

namespace Common.Users
{
    public class UserServiceFactory
    {
        public static IUserService GetUserService(DbConfig dbConfig)
        {
            switch (dbConfig.DataProvider.ToLower().Trim())
            {
                case "sqlserver": return new MsSql.UserService(dbConfig);
                case "mongodb": return new Mongo.UserService(dbConfig);
                case "postgres": return new PgSql.UserService(dbConfig);

                default: return new Mongo.UserService(dbConfig);
            }
        }
    }
}
