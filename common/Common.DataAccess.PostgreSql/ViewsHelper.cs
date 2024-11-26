using Common.DataAccess.RDBMS;
using Common.DataAccess.RDBMS.Model;
using Npgsql;
using Common.DataAccess.PostgreSql;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Common.DataAccess.PostgreSql
{
    public class ViewsHelper : IViewsHelper
    {
        private readonly string _connectionString;

        public ViewsHelper(string connectionString)
        {
            _connectionString = connectionString;
        }

        public List<string> GetViewNames()
        {
            var viewsDetailsList = GetViews();

            return viewsDetailsList.Select(view => view.SchemaName + "." + view.ViewName).ToList();
        }

        public List<DbView> GetViewsDetails(List<string> viewsList = null)
        {
            var views = GetViews(viewsList);

            foreach (var view in views)
            {
                view.Columns = GetColumns(view.SchemaName, view.ViewName);
            }

            return views;
        }

        public async void CreateView(string viewName, string sqlQuery)
        {
            var query = "CREATE VIEW " + viewName + " AS " + sqlQuery;

            DeleteView(viewName);

            await new QueryHelper(_connectionString).ExecuteScalar(query, null);
        }

        public async void DeleteView(string viewName)
        {
            var query = "DROP VIEW " + viewName;

            var isExistingView = GetViewNames().Contains(viewName);
            if (!isExistingView) return;

            await new QueryHelper(_connectionString).ExecuteScalar(query, null);
        }

        private List<DbView> GetViews(ICollection<string> viewsList = null)
        {
            DataTable tblViews;
            var views = new List<DbView>();
            //var database = new DbHelper().GetDatabase(_connectionString);
            var serverName = new DbHelper().GetServerName(_connectionString);

            using (var connection = new NpgsqlConnection(_connectionString))
            {
                connection.Open();
                tblViews = connection.GetSchema();
                connection.Close();
            }

            foreach (DataRow view in tblViews.Rows)
            {
                var fullyQualifiedName = view["TABLE_CATALOG"] + "." + view["TABLE_SCHEMA"] + "." + view["TABLE_NAME"];

                if (viewsList == null || viewsList.Contains(fullyQualifiedName))
                {
                    views.Add(new DbView()
                    {
                        Server = serverName,
                        DatabaseName = view["TABLE_CATALOG"].ToString(),
                        SchemaName = view["TABLE_SCHEMA"].ToString(),
                        ViewName = view["TABLE_NAME"].ToString()
                    });
                }
            }

            return views;
        }

        private List<DbColumn> GetColumns(string schema, string viewName)
        {
            DataTable columns;
            var lstColumns = new List<DbColumn>();

            using (var connection = new NpgsqlConnection(_connectionString))
            {
                connection.Open();
                columns = connection.GetSchema(viewName);
                connection.Close();
            }

            foreach (DataRow item in columns.Rows)
            {
                var column = new DbColumn
                {
                    ColumnName = item["COLUMN_NAME"].ToString(),
                    DataType = item["DATA_TYPE"].ToString()
                };

                lstColumns.Add(column);
            }

            return lstColumns;

        }

    }
}