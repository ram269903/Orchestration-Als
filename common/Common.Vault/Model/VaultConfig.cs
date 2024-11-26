namespace Common.Vault.Model
{
    public class VaultConfig
    {
        public string HostIp { get; set; }
        public int Port { get; set; }
        public int RendererPort { get; set; }
        public string Database { get; set; }
        public string StagingFolder { get; set; }
        public string DropFolder { get; set; }
        public string NetworkUserId { get; set; }
        public string NetworkPassword { get; set; }
        public string VaultDownloadFolder { get; set; }
    }
}
