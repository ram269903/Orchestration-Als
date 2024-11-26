
namespace DSS.Als.Orchestration.Models
{
    public class PrintSettings
    {
        public string? PGPFTPScriptPath { get; set; }
        public string? SAPGPFTPScriptPath { get; set; }

        public string? PGPPublicKeyPath { get; set; }

        public SftpSettings SftpSettings { get; set; }
    }

    public class SftpSettings
    {
        public string SftpServerIp { get; set; }
        public int SftpServerPort { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        //public string SftpServerPath { get; set; }

        public bool PV1 { get; set; }

        public string PV1SFTP { get; set; }

        public bool PV2 { get; set; }

        public string PV2SFTP { get; set; }

    }
}
