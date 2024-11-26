using System;
using System.Collections.Generic;

namespace DSS.Als.Orchestration.Models
{
    public class HAConfig
    {
        public string Products { get; set; }
        public Server SG { get; set; }
        public Dictionary<string, ProductSettings> ProductSettings { get; set; }
        public Server ArchiveServers { get; set; }
        public Server ArchiveDowloadFolderPaths { get; set; }

    }

    public class ProductSettings
    {
        public string Servers { get; set; }

    }

    public class Server
    {
        public string Server1 { get; set; }
        public string Server2 { get; set; }
        public string Server3 { get; set; }
    }
}
