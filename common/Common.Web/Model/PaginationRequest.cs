using Common.DataAccess;

namespace Common.Web.Model
{
    public class PaginationRequest
    {
        public int? Page { get; set; } = null;

        public int? PageSize { get; set; } = null;

        //public Search Search { get; set; }
        public SearchFilter Filter { get; set; } = null;

        public Order Order { get; set; }

        public string Fields { get; set; }

    }

    public class Order
    {
        public string OrderByProperty { get; set; }

        public SortOrder SortOrder { get; set; } = SortOrder.Descending;
    }

    //public class Search
    //{
    //    public string Value { get; set; }

    //    public string Regex { get; set; }
    //}
}
