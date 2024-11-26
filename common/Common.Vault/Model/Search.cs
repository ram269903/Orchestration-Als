using System.Collections.Generic;

namespace Common.Vault.Model
{
    public class SearchCriteria
    {
        public string Database { get; set; }
        public Dictionary<string, string> KeyValue { get; set; }
    }

    public class SearchResult
    {
        public string Matched { get; set; }
        public string Account { get; set; }
        public string Date { get; set; }
        public string Format { get; set; }
        public string File { get; set; }
        public string Offset { get; set; }
        public int Pages { get; set; }
    }
}
