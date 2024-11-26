using Common.DataAccess.RDBMS;
using Common.DataAccess.RDBMS.Model;
using MySql.Data.MySqlClient;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Common.DataAccess.MySql
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

            return viewsDetailsList.Select(view => view.DatabaseName + "." + view.ViewName).ToList();
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
            var database = new DbHelper().GetDatabase(_connectionString);
            var serverName = new DbHelper().GetServerName(_connectionString);

            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                tblViews = connection.GetSchema("Views");
                connection.Close();
            }

            foreach (DataRow view in tblViews.Rows)
            {
                if (view["TABLE_SCHEMA"].ToString() != database.DatabaseName) continue;
                var fullyQualifiedName = view["TABLE_SCHEMA"] + "." + view["TABLE_NAME"];

                if (viewsList == null || viewsList.Contains(fullyQualifiedName))
                {
                    views.Add(new DbView()
                    {
                        Server = serverName,
                        DatabaseName = view["TABLE_SCHEMA"].ToString(),
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

            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                columns = connection.GetSchema("Columns", new[] { null, schema, viewName, null });
                connection.Close();
            }

            foreach (DataRow item in columns.Rows)
            {
                var fullyQualifiedName = $"{item["TABLE_SCHEMA"]}.{item["TABLE_NAME"]}.{item["COLUMN_NAME"]}";

                var column = new DbColumn
                {
                    ColumnName = item["COLUMN_NAME"].ToString(),
                    DataType = item["DATA_TYPE"].ToString(),
                    FullyQualifiedName = fullyQualifiedName,
                };

                lstColumns.Add(column);
            }

            return lstColumns;

        }

    }
}
