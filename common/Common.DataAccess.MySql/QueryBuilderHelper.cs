﻿using Common.DataAccess.RDBMS;
using SqlKata.Compilers;
using System.Collections.Generic;
using System.Linq;

namespace Common.DataAccess.MySql
{
    public class QueryBuilderHelper : IQueryBuilderHelper
    {
        public string BuildQuery(RDBMS.Models.Query customQuery)
        {
            var compiler = new MySqlCompiler();

            var tables = new List<string>();
            var columns = new List<string>();

            foreach (var column in customQuery.Columns)
            {
                var table = column.DboName.Split('.')[1];
                var columnName = string.IsNullOrEmpty(column.AliasName) ? column.ColumnName : column.ColumnName + " As " + column.AliasName;
                
                if(column.Output == true)
                    columns.Add($"{table}.{columnName}");

                if (!tables.Contains(table))
                    tables.Add(table);
            }

            var mainTable = tables.FirstOrDefault();

            if (customQuery.Joins != null && customQuery.Joins.Count > 0)
            {
                mainTable = customQuery.Joins[0].LeftColumn.DboName.Split('.')[1];
            }

            var query = new SqlKata.Query(mainTable).Select(columns.ToArray());

            if (!string.IsNullOrEmpty(customQuery.WhereConditions))
            {
                query.WhereRaw(customQuery.WhereConditions);
            }

            if (customQuery.QueryOptions.IsDistinct)
                query.IsDistinct = true;

            if (customQuery.OrderBy != null && customQuery.OrderBy.Count > 0)
            {
                foreach (var item in customQuery.OrderBy)
                {
                    if (item.SortType.ToLower() == "desc" || item.SortType.ToLower() == "descending")
                        query.OrderByDesc(item.ColumnName);
                    else if (item.SortType.ToLower() == "asc" || item.SortType.ToLower() == "ascending")
                        query.OrderBy(item.ColumnName);
                }
            }

            foreach (var column in customQuery.Columns)
            {
                var table = column.DboName.Split('.')[1];
                var columnName = column.ColumnName;

                var orderBy = customQuery.OrderBy?.FirstOrDefault(x=>x.ColumnName == columnName);

                if (orderBy == null)
                {
                    if (!string.IsNullOrEmpty(column.SortType))
                    {
                        if (column.SortType.ToLower() == "desc")
                            query.OrderByDesc($"{table}.{columnName}");
                        else if (column.SortType.ToLower() == "asc")
                            query.OrderBy($"{table}.{columnName}");
                    }
                }
            }

            if (customQuery.Page != null && customQuery.PageSize != null) 
            {
                var offset = (int)customQuery.PageSize * ((int)customQuery.Page - 1);

                query.Limit((int)customQuery.PageSize).Offset(offset);
            }

            if (customQuery.Joins!=null && customQuery.Joins.Count > 0)
            {
                foreach (var tableJoin in customQuery.Joins)
                {
                    var leftTable = tableJoin.LeftColumn.DboName.Split('.')[1];
                    var rightTable = tableJoin.RightColumn.DboName.Split('.')[1];

                    var rightColumnName = $"{rightTable}.{tableJoin.RightColumn.ColumnName}";
                    var leftColumnName = $"{leftTable}.{tableJoin.LeftColumn.ColumnName}";

                    var op = tableJoin.Criteria.ToLower() == "equals" ? "=" : "<>";

                    switch (tableJoin.JoinType.ToLower())
                    {
                        case "leftjoin":
                            query.LeftJoin(rightTable, join => join.On(leftColumnName, rightColumnName, op));
                            break;
                        case "rightjoin":
                            query.RightJoin(rightTable, join => join.On(leftColumnName, rightColumnName, op));
                            break;
                        case "crossjoin":
                            query.CrossJoin(rightTable);
                            break;
                        case "innerjoin":
                            query.Join(rightTable, join => join.On(leftColumnName, rightColumnName, op));
                            break;
                        default:
                            break;
                    }
                }
                
            }
            
            
            var result = compiler.Compile(query);

            var sql = result.ToString();

            return sql;

            //var empDeptQuery = new Query("employee");
            //empDeptQuery.Select("employee.Name", "dept.Deptname");
            //empDeptQuery.Join("dept", join => join.On("employee.deptid", "dept.deptid"));
        }
    }
}