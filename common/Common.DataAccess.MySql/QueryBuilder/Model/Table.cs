using System.Text;

namespace Common.DataAccess.MsSql.QueryBuilder.Model
{
    public class Table
    {
        public string Server { get; set; }
        public string Database { get; set; }
        public string Schema { get; set; }
        public string TableName { get; set; }
        public string TableAliasName { get; set; }

        public Table(string server, string database, string schema, string tableName, string tableAliasName)
        {
            Server = server;
            Database = database;
            Schema = schema;
            TableName = tableName;

            TableAliasName = string.IsNullOrEmpty(tableAliasName) ? tableName : tableAliasName;
        }

        public override string ToString()
        {
            var stringBuilder = new StringBuilder();

            //if (!string.IsNullOrEmpty(Server))
            //    stringBuilder.Append(Server).Append(".");

            //if (!string.IsNullOrEmpty(Database))
            //    stringBuilder.Append(Database).Append(".");

            stringBuilder.Append("[").Append(Schema).Append("]");

            stringBuilder.Append(".");

            stringBuilder.Append("[").Append(TableName).Append("]");

            return stringBuilder.ToString();
        }

        public string GetTableWithAliasName()
        {
            var stringBuilder = new StringBuilder();

            stringBuilder.Append(ToString());

            stringBuilder.Append(" As ");
            stringBuilder.Append("[").Append(TableAliasName).Append("]");

            return stringBuilder.ToString();
        }
    }
}
