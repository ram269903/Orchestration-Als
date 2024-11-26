using System.Collections.Generic;

namespace Common.DataAccess.Teradata
{
    public static class ExpressionBuilder
    {
        public static string BuildWhereExpression(SearchFilter searchFilter)
        {
            var conditions = new List<string>();

            foreach (var filter in searchFilter.Filters)
            {
                var searchStringValue = filter.Value.ToString().Replace("%", @"\%").Replace("_", @"\_");

                var value = GetFilterValue(filter.Value);

                switch (filter.Operator)
                {
                    case Operator.Equals:
                        if (filter.CaseSensitive)
                            conditions.Add($"{filter.PropertyName} = {value}");
                        else
                            conditions.Add($"lower({filter.PropertyName}) = lower({value})");
                        break;
                    case Operator.GreaterThan:
                        conditions.Add($"{filter.PropertyName} > {value}");
                        break;
                    case Operator.LessThan:
                        conditions.Add($"{filter.PropertyName} < {value}");
                        break;
                    case Operator.GreaterThanOrEqual:
                        conditions.Add($"{filter.PropertyName} >= {value}");
                        break;
                    case Operator.LessThanOrEqual:
                        conditions.Add($"{filter.PropertyName} <= {value}");
                        break;
                    case Operator.Contains:
                        if (filter.CaseSensitive)
                            conditions.Add($"{filter.PropertyName} LIKE '%{searchStringValue}%'");
                        else
                            conditions.Add($"lower({filter.PropertyName}) LIKE '%{searchStringValue.ToLower()}%'");
                        break;
                    case Operator.StartsWith:
                        conditions.Add($"lower({filter.PropertyName}) LIKE '{searchStringValue}%'");
                        break;
                    case Operator.EndsWith:
                        conditions.Add($"lower({filter.PropertyName}) LIKE '%{searchStringValue}'");
                        break;
                    case Operator.NotEquals:
                        if (filter.CaseSensitive)
                            conditions.Add($"lower({filter.PropertyName}) <> lower({value})");
                        else
                            conditions.Add($"{filter.PropertyName} <> {value}");
                        break;
                    default:
                        break;
                }
                
            }

            return string.Join($" {searchFilter.ConditionOperator.ToString()} ", conditions);

        }

        private static string GetFilterValue(object filterValue)
        {
            var value = string.Empty;
            var dataType = filterValue.GetType().ToString();

            if (dataType == "System.String" || dataType == "System.DateTime")
                value = $"'{filterValue}'";
            else if (dataType == "System.Boolean")
                value = filterValue.ToString().ToLower();
            else
                value = $"{filterValue}";

            return value;
        }
    }
}
