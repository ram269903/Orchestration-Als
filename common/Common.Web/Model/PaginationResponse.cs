
namespace Common.Web.Model
{
    public class PaginationResponse : ResponseMessage
    {
        public long? RecordCount { get; set; }
        public dynamic MiscData { get; set; } 
     
    }
}
