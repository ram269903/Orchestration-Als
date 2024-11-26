
using System.Collections.Generic;

namespace Common.Config
{
    public class ApiSetting
    {
        public int? RetryInterval { get; set; }
        public int? RetriesCount { get; set; }
        public string Url { get; set; }
        public string Method { get; set; }
        public ServiceType ServiceType { get; set; }
        public Dictionary<string, string> Headers { get; set; }
        public Dictionary<string, string> Parameters { get; set; }
    }

    public enum ServiceType
    {
        RestApi = 0,
        Soap = 1
    }
}
