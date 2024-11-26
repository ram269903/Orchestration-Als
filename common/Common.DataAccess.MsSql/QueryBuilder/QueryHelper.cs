using System;
using System.Text;
using Common.DataAccess.MsSql.QueryBuilder.Model;

namespace Common.DataAccess.MsSql.QueryBuilder
{
    public static class QueryHelper
    {
        internal static string GetComparisonOperatorString(Comparison comparisonOperator) 
        {
            var operatorString = string.Empty;

            switch (comparisonOperator)
            {
                case Comparison.Equals:
                    operatorString = " = "; break;
                case Comparison.NotEquals:
                    operatorString = " <> "; break;
                case Comparison.GreaterThan:
                    operatorString = " > "; break;
                case Comparison.GreaterOrEquals:
                    operatorString = " >= "; break;
                case Comparison.LessThan:
                    operatorString = " < "; break;
                case Comparison.LessOrEquals:
                    operatorString = " <= "; break;
            }

            return operatorString;
        }

        internal static string CreateComparisonClause(Column column, Comparison comparisonOperator, object value)
        {
            var stringBuilder = new StringBuilder();
            var columnName = column.ToString();

            if (value != null && value != DBNull.Value)
            {
                var sqlValue = FormatSqlValue(value);

                switch (comparisonOperator)
                {
                    case Comparison.Equals:
                        stringBuilder.Append(columnName);
                        stringBuilder.Append(" = ");
                        stringBuilder.Append(sqlValue); 
                        break;
                    case Comparison.NotEquals:
                        stringBuilder.Append(columnName);
                        stringBuilder.Append(" <> ");
                        stringBuilder.Append(sqlValue); 
                        break;
                    case Comparison.GreaterThan:
                        stringBuilder.Append(columnName);
                        stringBuilder.Append(" > ");
                        stringBuilder.Append(sqlValue); 
                        break;                        
                    case Comparison.GreaterOrEquals:
                        stringBuilder.Append(columnName);
                        stringBuilder.Append(" >= ");
                        stringBuilder.Append(sqlValue); 
                        break;                        
                    case Comparison.LessThan:
                        stringBuilder.Append(columnName);
                        stringBuilder.Append(" < ");
                        stringBuilder.Append(sqlValue); 
                        break;                         
                    case Comparison.LessOrEquals:
                        stringBuilder.Append(columnName);
                        stringBuilder.Append(" <= ");
                        stringBuilder.Append(sqlValue); 
                        break;                        
                    case Comparison.Like:
                        stringBuilder.Append(columnName);
                        stringBuilder.Append(" LIKE ");
                        stringBuilder.Append(sqlValue); 
                        break;                         
                    case Comparison.NotLike:
                        stringBuilder.Append(" NOT ");
                        stringBuilder.Append(columnName);
                        stringBuilder.Append(" LIKE ");
                        stringBuilder.Append(sqlValue); 
                        break; 
                    case Comparison.In:
                        stringBuilder.Append(columnName);
                        stringBuilder.Append(" NOT ( ");
                        stringBuilder.Append(sqlValue);
                        stringBuilder.Append(" )");
                        break;
                }
            }
            else // value==null	|| value==DBNull.Value
            {
                if ((comparisonOperator != Comparison.Equals) && (comparisonOperator != Comparison.NotEquals))
                {
                    throw new Exception("Cannot use comparison operator " + comparisonOperator.ToString() + " for NULL values.");
                }
                else
                {
                    switch (comparisonOperator)
                    {
                        case Comparison.Equals:
                            stringBuilder.Append(columnName);
                            stringBuilder.Append(" IS NULL ");
                            break;
                        case Comparison.NotEquals:
                            stringBuilder.Append(" NOT ");
                            stringBuilder.Append(columnName);
                            stringBuilder.Append(" IS NULL ");
                            break;
                    }
                }
            }

            return stringBuilder.ToString();
        }

        internal static string FormatSqlValue(object value)
        {
            var stringBuilder = new StringBuilder();

            if (value == null)
            {
                stringBuilder.Append("NULL");
            }
            else
            {
                switch (value.GetType().Name)
                {
                    case "String": 
                        stringBuilder.Append("'");
                        stringBuilder.Append(((string)value).Replace("'", "''"));
                        stringBuilder.Append("'"); 
                        break;
                    case "DateTime": 
                        stringBuilder.Append("'");
                        stringBuilder.Append(((DateTime)value).ToString("yyyy/MM/dd hh:mm:ss"));
                        stringBuilder.Append("'");
                        break;
                    case "DBNull": 
                        stringBuilder.Append("NULL"); 
                        break;
                    case "Boolean": 
                        stringBuilder.Append((bool)value ? "1" : "0"); 
                        break;
                    default: 
                        stringBuilder.Append(value); 
                        break;
                }
            }
            
            return stringBuilder.ToString();
        }
    }
}
