using Common.Config;

namespace Common.Roles
{
    public class RoleServiceFactory
    {
        public static IRoleService GetRoleService(DbConfig dbConfig)
        {
            switch (dbConfig.DataProvider.ToLower().Trim())
            {
                case "sqlserver": return new MsSql.RoleService(dbConfig);
                case "mongodb": return new Mongo.RoleService(dbConfig);
                case "postgres": return new PgSql.RoleService(dbConfig);

                default: return new Mongo.RoleService(dbConfig);
            }

            //object o = null;

            //try
            //{
            //    Assembly assembly = Assembly.LoadFrom($"Common.Roles.{dbConfig.DataProvider}.dll");

            //    foreach (var t in assembly.GetTypes())
            //    {
            //        if (t.Name == "RoleService")
            //            o = Activator.CreateInstance(t);
            //    }

            //    return (IRoleService) o;
            //}
            //catch (System.Exception)
            //{
            //    throw;
            //}
        }
    }
}
