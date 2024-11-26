using Common.DataAccess.MsSql.QueryBuilder.Model;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Common.DataAccess.MsSql.QueryBuilder
{
    public class SelectQueryBuilder
    {
        private Dictionary<string, Table> _selectedTables = new Dictionary<string, Table>();
        private Dictionary<string, Column> _selectedColumns = new Dictionary<string, Column>();
        private List<Join> _joins = new List<Join>();
        private WhereStatement _where = new WhereStatement();
        private Dictionary<string, Column> _groupByColumns = new Dictionary<string, Column>();
        private Dictionary<string, OrderByClause> _orderBy = new Dictionary<string, OrderByClause>();
        public bool GetOnlyRecordsProcessed = false;

        //Temp Solution - need to remove
        private string _whereCondition;

        //public bool Distinct { get; set; }
        //public int? Top { get; set; }
        //public bool TopPercent { get; set; }

        public SelectOptions SelectOptions { get; set; }

        public Dictionary<string, Table> Tables
        {
            get { return _selectedTables; }
            //set { _selectedTables = value; }
        }

        public void AddTable(Table table)
        {
            var key = table.ToString();

            if (!_selectedTables.ContainsKey(key))
                _selectedTables.Add(key, table);
        }

        public void AddTables(List<Table> tables)
        {
            foreach (var table in tables)
            {
                var key = table.ToString();

                if (!_selectedTables.ContainsKey(key))
                    _selectedTables.Add(key, table);
            }
        }

        public void AddColumn(Column column)
        {
            var key = column.ToString();

            if (!_selectedColumns.ContainsKey(key))
                _selectedColumns.Add(key, column);
        }

        public void AddColumns(List<Column> columns)
        {
            foreach (var column in columns)
            {
                var key = column.ToString();

                if (!_selectedColumns.ContainsKey(key))
                    _selectedColumns.Add(key, column);
            }
        }

        public void SelectAllColumns()
        {
            _selectedColumns.Clear();
        }

        public void AddJoin(Join join)
        {
            _joins.Add(join);
        }

        public void AddJoin(JoinType joinType, Table joinTable, Column toColumn, Comparison @operator, Column fromColumn)
        {
            var tableKey = joinTable.ToString();

            if (_selectedTables.ContainsKey(tableKey))
                _selectedTables.Remove(tableKey);

            var join = new Join(joinType, joinTable, toColumn, @operator, fromColumn);
            AddJoin(join);
        }

        //Temp Solution - need to remove
        public void AddWhereCondition(string whereCondition)
        {
            _whereCondition = whereCondition;
        }

        public void AddWhere(Column column, Comparison @operator, object value)
        {
            _where.AddWhere(new Expression(column, @operator, value));
        }

        public void AddWhere(Expression expression)
        {
            _where.AddWhere(expression, 0);
        }

        public void AddWhere(Column column, Comparison @operator, object value, int level, LogicOperator condition = LogicOperator.And)
        {
            _where.AddWhere(new Expression(column, @operator, value), level, condition);
        }

        public void AddWhere(Expression expression, int level, LogicOperator condition = LogicOperator.And)
        {
            _where.AddWhere(expression, level, condition);
        }

        public void AddWhere(List<Expression> expressions)
        {
            _where.AddWhere(expressions);
        }

        public void AddWhere(List<Expression> expressions, int level, LogicOperator condition = LogicOperator.And)
        {
            _where.AddWhere(expressions, level, condition);
        }

        public void AddGroupByColumn(Column column)
        {
            var key = column.ToString();

            if (!_groupByColumns.ContainsKey(key))
                _groupByColumns.Add(key, column);
        }

        public void AddGroupByColumn(List<Column> columns)
        {
            foreach (var column in columns)
            {
                var key = column.ToString();

                if (!_groupByColumns.ContainsKey(key))
                    _groupByColumns.Add(key, column);
            }
        }

        public void AddOrderByColumn(Column column, SortType sortType = SortType.Asc)
        {
            var key = column.ToString();

            if (!_orderBy.ContainsKey(key))
                _orderBy.Add(key, new OrderByClause(column, sortType));
        }

        public string BuildQuery()
        {
            var stringBuilder = new StringBuilder();

            stringBuilder.Append("SELECT ");

            // Output Distinct
            //if (Distinct)
            //{
            //    stringBuilder.Append("DISTINCT ");
            //}
            if (SelectOptions != null)
            {
                stringBuilder.Append(SelectOptions.RowsSelection == RowsSelection.Distinct ? "DISTINCT " : "ALL ");

                // Output Top clause
                if (SelectOptions.Top != null && SelectOptions.Top.Value != null)
                {
                    stringBuilder.Append("TOP ").Append(SelectOptions.Top.Value).Append(" ");

                    if (SelectOptions.Top.IsPercent == true)
                        stringBuilder.Append("PERCENT ");
                }
            }

            // Output column names
            if (GetOnlyRecordsProcessed)
            {
                stringBuilder.Append("Count(*) Count");
            }
            else if (_selectedColumns.Count == 0)
            {
                if (_selectedTables.Count == 1)
                {
                    stringBuilder.Append("[");
                    stringBuilder.Append(_selectedTables.First().Value.TableAliasName);
                    stringBuilder.Append("]");
                    stringBuilder.Append(".");
                }

                stringBuilder.Append("*");
            }
            else
            {
                foreach (var column in _selectedColumns.Values)
                {
                    stringBuilder.Append(column.GetColumnWithAliasName());
                    stringBuilder.Append(", ");
                }

                stringBuilder.Remove(stringBuilder.Length - 2, 2);
            }

            // Output table names
            if (_selectedTables.Count > 0)
            {
                stringBuilder.Append(" FROM ");
                foreach (Table table in _selectedTables.Values)
                {
                    stringBuilder.Append(table.GetTableWithAliasName());
                    stringBuilder.Append(", ");
                }

                stringBuilder.Remove(stringBuilder.Length - 2, 2);
            }

            // Output joins
            if (_joins.Count > 0)
            {
                foreach (var join in _joins)
                {
                    stringBuilder.Append(@join);
                }
            }

            //// Output where statement
            //if (_where.Count > 0)
            //{
            //    stringBuilder.Append(" WHERE ");
            //    stringBuilder.Append(_where);
            //}

            //Temp Solution - need to remove
            if (!string.IsNullOrEmpty(_whereCondition))
            {
                stringBuilder.Append(" WHERE ");
                stringBuilder.Append(_whereCondition);
            }

            // Output GroupBy statement
            if (_groupByColumns.Count > 0)
            {
                stringBuilder.Append(" GROUP BY ");

                foreach (Column column in _groupByColumns.Values)
                {
                    stringBuilder.Append(column);
                    stringBuilder.Append(", ");
                }

                stringBuilder.Remove(stringBuilder.Length - 2, 2);
            }


            // Output OrderBy statement
            if (_orderBy.Count > 0)
            {
                stringBuilder.Append(" ORDER BY ");

                foreach (OrderByClause clause in _orderBy.Values)
                {
                    stringBuilder.Append(clause);
                    stringBuilder.Append(", ");
                }

                stringBuilder.Remove(stringBuilder.Length - 2, 2);
                stringBuilder.Append(" ");
            }
            else
            {
                stringBuilder.Append(" ORDER BY [" + _selectedColumns.FirstOrDefault().Value.ColumnName + "]");
            }

            //// Output Top clause
            //if (SelectOptions.Top != null && SelectOptions.Top.Value != null)
            //{
            //    //stringBuilder.Append("TOP ").Append(SelectOptions.Top.Value).Append(" ");

            //    stringBuilder.Append($" OFFSET 0 ROWS FETCH FIRST {SelectOptions.Top.Value} ROWS ONLY");

            //    //if (SelectOptions.Top.IsPercent == true)
            //    //    stringBuilder.Append("PERCENT ");
            //}

            return stringBuilder.ToString();
        }
    }
}
