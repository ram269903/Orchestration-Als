using Common.DataAccess.RDBMS;

namespace Common.DataAccess.Factory
{
    public class DbFactories
    {
        public static IDbHelper GetDbFactory(string dataProvider)
        {
            switch (dataProvider.ToLower().Trim())
            {
                case "sqlserver": return new MsSql.DbHelper();
                case "postgresql": return new PostgreSql.DbHelper();
                case "mysql": return new MySql.DbHelper();
                case "teradata": return new Teradata.DbHelper();

                default: return new MsSql.DbHelper();
            }
        }

        public static ITablesHelper GetTablesFactory(string dataProvider, string connectionString)
        {
            switch (dataProvider.ToLower().Trim())
            {
                case "sqlserver": return new MsSql.TablesHelper(connectionString); ;
                case "postgresql": return new PostgreSql.TablesHelper(connectionString);
                case "mysql": return new MySql.TablesHelper(connectionString);
                case "teradata": return new Teradata.TablesHelper(connectionString);

                default: return new MsSql.TablesHelper(connectionString);
            }
        }

        public static IViewsHelper GetViewsFactory(string dataProvider, string connectionString)
        {
            switch (dataProvider.ToLower().Trim())
            {
                case "sqlserver": return new MsSql.ViewsHelper(connectionString); ;
                case "postgresql": return new PostgreSql.ViewsHelper(connectionString);
                case "mysql": return new MySql.ViewsHelper(connectionString);
                case "teradata": return new Teradata.ViewsHelper(connectionString);

                default: return new MsSql.ViewsHelper(connectionString);
            }
        }

        public static IQueryHelper GetQueryFactory(string dataProvider, string connectionString)
        {
            switch (dataProvider.ToLower().Trim())
            {
                case "sqlserver": return new MsSql.QueryHelper(connectionString); ;
                case "postgresql": return new PostgreSql.QueryHelper(connectionString);
                case "mysql": return new MySql.QueryHelper(connectionString);
                case "teradata": return new Teradata.QueryHelper(connectionString);

                default: return new MsSql.QueryHelper(connectionString);
            }
        }
        public static IQueryBuilderHelper GetQueryBuilderHelper(string dataProvider)
        {
            switch (dataProvider.ToLower().Trim())
            {
                case "sqlserver": return new MsSql.QueryBuilderHelper();
                //case "postgresql": return new PostgreSql.QueryBuilderHelper();
                case "mysql": return new MySql.QueryBuilderHelper();
                case "teradata": return new Teradata.QueryBuilderHelper();

                default: return new MsSql.QueryBuilderHelper();
            }
        }
    }
}
