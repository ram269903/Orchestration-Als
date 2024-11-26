using System.Collections.Generic;
using static PB.Common.Services.ServiceExtensions;

namespace PB.Common.Services.Model
{
    public class ApiInfo
    {
        public int? RetryInterval { get; set; }
        public int? RetriesCount { get; set; }
        public string Url { get; set; }
        //public string Method { get; set; }
        public HttpContentType ContentType { get; set; }
        public ServiceType ServiceType { get; set; }
        public Dictionary<string, string> Header { get; set; }
        public Dictionary<string, string> Parameters { get; set; }
    }

    public enum ServiceType
    {
        RestApi = 0,
        Soap = 1
    }
}
