using System.Text;

namespace Common.DataAccess.MsSql.QueryBuilder.Model
{
    public class Column
    {
        public string TableAliasName { get; set; }
        public string ColumnName { get; set; }
        public string ColumnAliasName { get; set; }
        public string AggregatingFunction { get; set; }

        public Column(string tableAliasName, string columnName, string columnAliasName = null, string aggregatingFunction = "")
        {
            TableAliasName = tableAliasName.Trim();
            ColumnName = columnName.Trim();
            AggregatingFunction = aggregatingFunction.Trim();
            ColumnAliasName = string.IsNullOrEmpty(columnAliasName) ? columnName.Trim() : columnAliasName.Trim();
        }

        public override string ToString()
        {
            var stringBuilder = new StringBuilder();

            if (!string.IsNullOrEmpty(AggregatingFunction))
            {
                stringBuilder.Append(AggregatingFunction);
                stringBuilder.Append("(");

                if (!string.IsNullOrEmpty(TableAliasName))
                {
                    stringBuilder.Append("[").Append(TableAliasName).Append("]").Append(".");
                }

                //stringBuilder.Append(ColumnName);
                stringBuilder.Append("[").Append(ColumnName).Append("]");
                stringBuilder.Append(")");
            }
            else
            {
                if (!string.IsNullOrEmpty(TableAliasName))
                {
                    stringBuilder.Append("[").Append(TableAliasName).Append("]").Append(".");
                }

                //stringBuilder.Append(ColumnName);
                stringBuilder.Append("[").Append(ColumnName).Append("]");
            }
 
            return stringBuilder.ToString();
        }

        public string GetColumnWithAliasName()
        {
            var stringBuilder = new StringBuilder();

            stringBuilder.Append(ToString());

            stringBuilder.Append(" As ");
            stringBuilder.Append("[").Append(ColumnAliasName).Append("]");

            return stringBuilder.ToString();
        }
    }
}
