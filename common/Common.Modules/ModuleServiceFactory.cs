using Common.Config;

namespace Common.Modules
{
    public class ModuleServiceFactory
    {

        public static IModuleService GetModuleService(DbConfig dbConfig)
        {
            switch (dbConfig.DataProvider.ToLower().Trim())
            {
                case "sqlserver": return new MsSql.ModuleService(dbConfig);
                case "mongodb": return new Mongo.ModuleService(dbConfig);
                case "postgres": return new PgSql.ModuleService(dbConfig);

                default: return new Mongo.ModuleService(dbConfig);
            }
        }

    }
}
