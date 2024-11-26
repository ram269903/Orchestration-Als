using Common.DataAccess.MsSql.QueryBuilder.Model;
using System.Collections.Generic;
using System.Text;

namespace Common.DataAccess.MsSql.QueryBuilder
{
    public class Join
    {
        public JoinType JoinType { get; set; }
        public Table JoinTable { get; set; }

        //public Table FromTable { get; set; }
        private readonly List<JoinCondition> _joinConditions = new List<JoinCondition>();

        public Join(JoinType join, Table joinTable, Column toColumn, Comparison @operator, Column fromColumn)
        {
            JoinType = join;
            JoinTable = joinTable;
            
            _joinConditions.Add(new JoinCondition (null, toColumn, @operator, fromColumn));
        }

        public void AddJoinCondition(LogicOperator logicOperator, Column toColumn, Comparison @operator, Column fromColumn)
        {
            _joinConditions.Add(new JoinCondition(logicOperator, toColumn, @operator, fromColumn));
        }

        public override string ToString()
        {
            var joinString = new StringBuilder();

            switch (JoinType)
            {
                case JoinType.InnerJoin: joinString.Append(" INNER JOIN "); break;
                case JoinType.FullJoin: joinString.Append(" FULL JOIN "); break;
                case JoinType.LeftJoin: joinString.Append(" LEFT JOIN "); break;
                case JoinType.RightJoin: joinString.Append(" RIGHT JOIN "); break;
            }

            joinString.Append(JoinTable.GetTableWithAliasName());

            joinString.Append(" ON ");

            foreach (var item in _joinConditions)
            {
                joinString.Append(item);
                //joinString.Append(" AND ");
            }

            //joinString.Remove(joinString.Length - 5, 5);

            joinString.Append(" ");

            return joinString.ToString();
        }
    }
}