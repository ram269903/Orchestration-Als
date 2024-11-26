using System.Collections.Generic;

namespace Common.Ftp
{
    public class FtpConfig
    {
        public string HostIp { get; set; }
        public int Port { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public bool EnableSsl { get; set; }
        public string StagingFolder { get; set; }
        public Dictionary<string,string> DropFolders { get; set; }
        public string PickupFolder { get; set; }
    }
}
