using System;
using System.Collections.Generic;

namespace Common.DataAccess
{
    [Serializable]
    public class SearchFilter
    {
        public ConditionOperator ConditionOperator { get; set; } = ConditionOperator.AND;
        public IList<Filter> Filters { get; set; }
    }

    [Serializable]
    public class Filter
    {
        public string PropertyName { get; set; }
        public Operator Operator { get; set; } = Operator.Equals;
        public object Value { get; set; }
        public bool CaseSensitive { get; set; }
        public string DataType { get; set; }
    }

    [Serializable]
    public enum Operator
    {
        Equals,
        GreaterThan,
        LessThan,
        GreaterThanOrEqual,
        LessThanOrEqual,
        Contains,
        StartsWith,
        EndsWith,
        NotEquals,
        IsNull,
        IsNotNull,
        IsEmpty,
        IsNotEmpty,
        DoesNotContain,
        Between,
        In,
        NotIn,
        NotBetween,
        DoesNotEndWith,
        DoesNotStartWith

    }

    [Serializable]
    public enum ConditionOperator
    {
        AND,
        OR
    }

    public enum SortOrder
    {
        NoSort = 0,
        Ascending = 1,
        Descending = -1
    }
}
