using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Common.DataAccess.MsSql.QueryBuilder
{
    public class WhereStatement
    {
        private SortedDictionary<int, ExpressionGroup> _whereConditions = new SortedDictionary<int, ExpressionGroup>();

        public double Count
        {
            get { return _whereConditions.Count; }
        }

        public void AddWhere(Expression expression)
        {
            AddWhere(expression, 0);
        }

        public void AddWhere(Expression expression, int level, LogicOperator condition = LogicOperator.And)
        {
            if (_whereConditions.ContainsKey(level))
            {
                _whereConditions[level].ExpressionList.Add(expression);
            }
            else
            {
                var expressionGroup = new ExpressionGroup() { 
                    Condition = condition,
                    ExpressionList = new List<Expression> { expression }
                };
            }
        }

        public void AddWhere(List<Expression> expressions)
        {
            AddWhere(expressions, 0, LogicOperator.And);
        }

        public void AddWhere(List<Expression> expressions, int level, LogicOperator condition = LogicOperator.And)
        {
            if (_whereConditions.ContainsKey(level))
            {
                _whereConditions[level].ExpressionList.AddRange(expressions);
            }
            else
            {
                var expressionGroup = new ExpressionGroup()
                {
                    Condition = condition,
                    ExpressionList = new List<Expression> (expressions)
                };
            }
        }

        public override string ToString()
        {
            var stringBuilder = new StringBuilder();

            foreach (var level in _whereConditions.Keys)
            {
                stringBuilder.Append(_whereConditions[level].ExpressionList);

                if (level != _whereConditions.Last().Key)
                {
                    switch (_whereConditions[level].Condition)
                    {
                        case LogicOperator.And:
                            stringBuilder.Append(" AND "); break;
                        case LogicOperator.Or:
                            stringBuilder.Append(" OR "); break;
                    }

                    stringBuilder.Append(" ( ");
                }
            }

            for (var i = 0; i <= _whereConditions.Count; i++)
            {
                stringBuilder.Append(" ) ");
            }

            return stringBuilder.ToString();
        }
    }
}

