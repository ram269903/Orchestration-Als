namespace Common.DataAccess.MsSql.QueryBuilder
{
    public enum Comparison
    {
        Equals,
        NotEquals,
        Like,
        NotLike,
        GreaterThan,
        GreaterOrEquals,
        LessThan,
        LessOrEquals,
        In
    }

    public enum JoinType
    {
        InnerJoin,
        FullJoin,
        LeftJoin,
        RightJoin
    }

    public enum LogicOperator
    {
        And,
        Or
    }

    public enum TopUnit
    {
        Records,
        Percent
    }

    public enum Aggrigate
    {
        Avg,
        Sum,
        Count,
        Max,
        Min
    }

    public enum SortType
    {
        None,
        Asc,
        Desc,
    }
}