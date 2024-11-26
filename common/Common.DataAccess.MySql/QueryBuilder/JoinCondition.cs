using Common.DataAccess.MsSql.QueryBuilder.Model;
using System.Text;

namespace Common.DataAccess.MsSql.QueryBuilder
{
    public class JoinCondition
    {
        public LogicOperator? LogicOperator { get; set; }
        public Column ToColumn { get; set; }
        public Comparison ComparisonOperator { get; set; }
        public Column FromColumn { get; set; }

        public JoinCondition(LogicOperator? logicOperator, Column toColumn, Comparison @operator, Column fromColumn)
        {
            LogicOperator = logicOperator;
            ToColumn = toColumn;
            ComparisonOperator = @operator;
            FromColumn = fromColumn;
        }

        public override string ToString()
        {
            var joinString = new StringBuilder();

            if (LogicOperator != null)
                joinString.Append(" " + LogicOperator.ToString() + " ");

            joinString.Append(ToColumn);
            joinString.Append(QueryHelper.GetComparisonOperatorString(ComparisonOperator));
            joinString.Append(FromColumn);

            return joinString.ToString();
        }
    }
}
