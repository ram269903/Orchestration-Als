using Common.DataAccess.MsSql.QueryBuilder.Model;

namespace Common.DataAccess.MsSql.QueryBuilder
{
    public class Expression
    {
        public Column Column { get; set; }
        public Comparison Operator { get; set; }
        public object Value { get; set; }

        public Expression(Column column, Comparison @operator, object value)
        {
            Column = column;
            Operator = @operator;
            Value = value;
        }

        public override string ToString()
        {
            return QueryHelper.CreateComparisonClause(Column, Operator, Value);
        }
    }
}
