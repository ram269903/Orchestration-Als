using Common.DataAccess.MsSql.QueryBuilder.Model;
using System.Text;

namespace Common.DataAccess.MsSql.QueryBuilder
{
    public class OrderByClause
    {
        public Column Column;
        public SortType SortType;
        
        public OrderByClause(Column column)
        {
            Column = column;
            SortType = SortType.Asc;
        }

        public OrderByClause(Column column, SortType sortType)
        {
            Column = column;
            SortType = sortType;
        }

        public override string ToString()
        {
            var orderStringBuilder = new StringBuilder();

            orderStringBuilder.Append(Column);

            switch (SortType)
            {
                case SortType.Asc:
                    orderStringBuilder.Append(" ASC"); break;
                case SortType.Desc:
                    orderStringBuilder.Append(" DESC"); break;
            }

            return orderStringBuilder.ToString();
        }
    }
}