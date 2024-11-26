//using System.Collections.Generic;

//namespace Common.DataAccess.MsSql.QueryBuilder.Model
//{
//    public class Query
//    {
//        public SelectOptions SelectOptions { get; set; }
//        public List<SelectedColumn> Columns { get; set; }
//        public List<SelectedColumn> Columns { get; set; }
//    }

//    public class SelectOptions
//    {
//        public RowsSelection RowsSelection { get; set; }
//        public Top Top { get; set; }
//        public GroupBy GroupBy { get; set; }
//    }

//    public enum RowsSelection
//    {
//        All,
//        Distinct
//    }

//    public class Top
//    {
//        public int? Value { get; set; }
//        public bool IsPercent { get; set; }
//    }

//    public class GroupBy
//    {
//        public bool All { get; set; }
//        public GroupByOptions GroupByOptions { get; set; }
//    }

//    public enum GroupByOptions
//    {
//        None,
//        WithCube,
//        WithRollUp
//    }

//    public class SelectedColumn
//    {
//        public bool Output { get; set; }
//        public string Server { get; set; }
//        public string Database { get; set; }
//        public string Schema { get; set; }
//        public string TableName { get; set; }
//        public string TableAliasName { get; set; }
//        public string ColumnName { get; set; }
//        public string ColumnAliasName { get; set; }
//        public bool GroupBy { get; set; }
//        public Aggrigate Aggrigate { get; set; }
//        public SortType SortType { get; set; }
//        public int? SortOrder { get; set; }

//    }

//    public enum Aggrigate
//    {
//        Avg,
//        Sum,
//        Count,
//        Max,
//        Min
//    }

//    public enum SortType
//    {
//        None,
//        Asc,
//        Desc,
//    }
//}
