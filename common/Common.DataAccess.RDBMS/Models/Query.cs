using System;
using System.Collections.Generic;

namespace Common.DataAccess.RDBMS.Models
{
    [Serializable]
    public class Query
    {
        public QueryOptions QueryOptions { get; set; }
        public List<DboColumn> DbColumns { get; set; }
        public List<Column> Columns { get; set; }
        public string WhereConditions { get; set; }
        public string Conditions { get; set; }
        public List<Join> Joins { get; set; }
        public List<Sort> OrderBy { get; set; }
        public int? Page { get; set; }
        public int? PageSize { get; set; }
    }

    [Serializable]
    public class QueryOptions
    {
        public bool IsDistinct { get; set; }
        public Top Top { get; set; }
    }

    [Serializable]
    public class Top
    {
        public int? Value { get; set; }
        public bool IsPercent { get; set; }
    }

    [Serializable]
    public class DboColumn
    {
        public string DatabaseId { get; set; }
        public string DatabaseName { get; set; }
        public string DboId { get; set; }
        public string DboName { get; set; }
        public string ColumnName { get; set; }
        public string DataType { get; set; }
        public string FullyQualifiedName { get; set; }
    }

    [Serializable]
    public class Column : DboColumn
    {
        public string AliasName { get; set; }
        public bool Output { get; set; }
        public bool GroupBy { get; set; }
        public string Aggrigate { get; set; }
        public string SortType { get; set; }

        public int Index { get; set; }
    }

    [Serializable]
    public class Join
    {
        public string JoinType { get; set; }
        public Column LeftColumn { get; set; }
        public string Criteria { get; set; }
        public Column RightColumn { get; set; }

        public int leftIndex { get; set; }
        public int rightIndex { get; set; }
    }

    [Serializable]
    public class Sort
    {
        public string ColumnName { get; set; }
        public string SortType { get; set; }
    }
}
