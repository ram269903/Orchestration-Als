
namespace Common.DataAccess.MsSql.QueryBuilder
{
    public class SelectOptions
    {
        public RowsSelection RowsSelection { get; set; }
        public Top Top { get; set; }
        public GroupBy GroupBy { get; set; }
    }

    public enum RowsSelection
    {
        All,
        Distinct
    }

    public class Top
    {
        public int? Value { get; set; }
        public bool IsPercent { get; set; }
    }

    public class GroupBy
    {
        public bool All { get; set; }
        public GroupByOptions GroupByOptions { get; set; }
    }

    public enum GroupByOptions
    {
        None,
        WithCube,
        WithRollUp
    }
}
