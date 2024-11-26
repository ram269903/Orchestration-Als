﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;

namespace Common.DataAccess.MySql
{
    public static class ExpressionBuilder
    {
        private static MethodInfo containsMethod = typeof(string).GetMethod("Contains", new[] { typeof(string) });
        private static MethodInfo startsWithMethod = typeof(string).GetMethod("StartsWith", new Type[] { typeof(string) });
        private static MethodInfo endsWithMethod = typeof(string).GetMethod("EndsWith", new Type[] { typeof(string) });
        //private static MethodInfo toLowerMethod = typeof(string).GetMethod("ToLower", new Type[] { });

        private static MethodInfo trimMethod = typeof(string).GetMethod("Trim", new Type[0]);
        private static MethodInfo toLowerMethod = typeof(string).GetMethod("ToLower", new Type[0]);

        public static Expression<Func<T, bool>> GetExpression<T>(SearchFilter searchFilters)
        {
            if (searchFilters.Filters.Count == 0)
                return null;

            var searchFilter = DeepClone<SearchFilter>(searchFilters);

            ParameterExpression param = Expression.Parameter(typeof(T), "t");
            Expression exp = null;

            if (searchFilter.Filters.Count == 1)
                exp = GetExpression<T>(param, searchFilter.Filters[0]);
            else if (searchFilter.Filters.Count == 2)
                exp = GetExpression<T>(param, searchFilter.Filters[0], searchFilter.Filters[1], searchFilter.ConditionOperator);
            else
            {
                while (searchFilter.Filters.Count > 0)
                {
                    var f1 = searchFilter.Filters[0];
                    var f2 = searchFilter.Filters[1];

                    if (exp == null)
                        exp = GetExpression<T>(param, searchFilter.Filters[0], searchFilter.Filters[1], searchFilter.ConditionOperator);
                    else
                    {
                        if (searchFilter.ConditionOperator == ConditionOperator.AND)
                            exp = Expression.AndAlso(exp, GetExpression<T>(param, searchFilter.Filters[0], searchFilter.Filters[1], searchFilter.ConditionOperator));
                        else
                            exp = Expression.Or(exp, GetExpression<T>(param, searchFilter.Filters[0], searchFilter.Filters[1], searchFilter.ConditionOperator));
                    }
                    searchFilter.Filters.Remove(f1);
                    searchFilter.Filters.Remove(f2);

                    if (searchFilter.Filters.Count == 1)
                    {
                        exp = Expression.AndAlso(exp, GetExpression<T>(param, searchFilter.Filters[0]));
                        searchFilter.Filters.RemoveAt(0);
                    }
                }
            }

            return Expression.Lambda<Func<T, bool>>(exp, param);
        }

        private static Expression GetExpression<T>(ParameterExpression param, Filter filter)
        {
            MemberExpression member = Expression.Property(param, filter.PropertyName);
            ConstantExpression constant = Expression.Constant(filter.Value);

            switch (filter.Operator)
            {
                case Operator.Equals:
                    return Expression.Equal(member, constant);
                //if (member.Type.Name == "String")
                //{
                //    var expToLower = Expression.Call(member, toLowerMethod);
                //    return Expression.Equal(expToLower, constant);
                //}
                //else
                //{
                //    return Expression.Equal(member, constant);
                //}
                case Operator.GreaterThan:
                    return Expression.GreaterThan(member, constant);

                case Operator.GreaterThanOrEqual:
                    return Expression.GreaterThanOrEqual(member, constant);

                case Operator.LessThan:
                    return Expression.LessThan(member, constant);

                case Operator.LessThanOrEqual:
                    return Expression.LessThanOrEqual(member, constant);

                case Operator.Contains:
                    return Expression.Call(member, containsMethod, constant);
                //var expToLowerContains = Expression.Call(member, toLowerMethod);
                //return Expression.Call(expToLowerContains, containsMethod, constant); // call StartsWith() on the exp, which is property.ToLower()

                case Operator.StartsWith:
                    return Expression.Call(member, startsWithMethod, constant);
                //var expToLowerStartsWith = Expression.Call(member, toLowerMethod);
                //return Expression.Call(expToLowerStartsWith, startsWithMethod, constant);

                case Operator.EndsWith:
                    return Expression.Call(member, endsWithMethod, constant);
                    //var expToLowerEndsWith = Expression.Call(member, toLowerMethod);
                    //return Expression.Call(expToLowerEndsWith, endsWithMethod, constant);
            }

            return null;
        }

        private static BinaryExpression GetExpression<T>(ParameterExpression param, Filter filter1, Filter filter2, ConditionOperator conditionOperator)
        {
            Expression bin1 = GetExpression<T>(param, filter1);
            Expression bin2 = GetExpression<T>(param, filter2);

            if (conditionOperator == ConditionOperator.AND)
                return Expression.AndAlso(bin1, bin2);
            else
                return Expression.Or(bin1, bin2);
        }

        private static T DeepClone<T>(T obj)
        {
            using (var ms = new MemoryStream())
            {
                var formatter = new BinaryFormatter();
                formatter.Serialize(ms, obj);
                ms.Position = 0;

                return (T)formatter.Deserialize(ms);
            }
        }
        public static string BuildWhereExpression(SearchFilter searchFilter)
        {
            var conditions = new List<string>();
            
            foreach (var filter in searchFilter.Filters)
            {
                var searchStringValue = filter.Value.ToString().Replace("%", @"[%]").Replace("_", @"[_]");

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
                            conditions.Add($"{filter.PropertyName} != {value}");
                        else
                            conditions.Add($"lower({filter.PropertyName}) != lower({value})");
                        break;
                    case Operator.IsNull:
                        conditions.Add($"{filter.PropertyName} IS NULL {value}");
                        break;
                    default:
                        break;
                }
                
            }

            return string.Join($" {searchFilter.ConditionOperator.ToString()} ", conditions);

        }

        private static string GetFilterValue(object filterValue)
        {
            var filterType = filterValue.GetType().ToString();

            string value;

            if (filterType == "System.String")
                value = $"'{filterValue.ToString()}'";
            else if (filterType == "System.DateTime")
                value = $"'{Convert.ToDateTime(filterValue).ToString("yyyy-MM-dd")}'";
            else
                value = $"{filterValue}";

            return value;
        }
    }
}