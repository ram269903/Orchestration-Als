//https://ourcodeworld.com/articles/read/369/how-to-access-a-sftp-server-using-ssh-net-sync-and-async-with-c-in-winforms
using Renci.SshNet;
using System.IO;

namespace Common.Ftp
{
    public class SFTPClient
    {
        private readonly string _host;
        private readonly int _port;
        private readonly string _userName;
        private readonly string _password;

        public SFTPClient(string host, int Port, string userName, string password)
        {
            _host = host.Trim();
            _port = Port;
            _userName = string.IsNullOrEmpty(userName.Trim()) ? "anonymous" : userName.Trim();
            _password = password.Trim();
        }

        public bool DownloadFile(string remoteFilePath, string targetFile)
        {
            var connectionInfo = new ConnectionInfo(_host, _userName, new PasswordAuthenticationMethod(_userName, _password));

            using (var client = new SftpClient(connectionInfo))
            {
                client.Connect();

                using (Stream fileStream = File.OpenWrite(targetFile))
                {
                    client.DownloadFile(remoteFilePath, fileStream);

                    client.Disconnect();
                }

            }

            return true;

        }

        public bool UploadFile(string ftpDirectory, string filePath)
        {
            var connectionInfo = new ConnectionInfo(_host, _userName, new PasswordAuthenticationMethod(_userName, _password));

            using (var client = new SftpClient(connectionInfo))
            {
                client.Connect();

                using (Stream fileStream = File.OpenRead(filePath))
                {
                    client.UploadFile(fileStream, ftpDirectory);

                    client.Disconnect();
                }
            }

            return true;
        }
    }
}
