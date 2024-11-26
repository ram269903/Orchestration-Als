namespace Common.Web.Model
{
    public class ResponseMessage
    {
        public string Status { get; set; }
        public string Message { get; set; }
        public object Data { get; set; }
        public object ErrorData { get; set; }
    }
}
