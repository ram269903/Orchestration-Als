using System.Collections.Generic;
using System.Text;

namespace Common.DataAccess.MsSql.QueryBuilder
{
    public class ExpressionGroup
    {
        public LogicOperator Condition { get; set; }
        public List<Expression> ExpressionList { get; set; }

        public override string ToString()
        {
            var stringBuilder = new StringBuilder();

            stringBuilder.Append(" ( ");

            foreach (var expression in ExpressionList)
            {
                switch (Condition)
                {
                    case LogicOperator.And:
                        stringBuilder.Append(" AND "); break;
                    case LogicOperator.Or:
                        stringBuilder.Append(" OR "); break;
                }

                stringBuilder.Append(expression);
            }

            stringBuilder.Append(" ) ");

            return stringBuilder.ToString();
        }
    }
}
