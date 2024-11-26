//http://www.codeguru.com/csharp/.net/net_security/authentication/article.php/c15051/ASPNET-FTP-with-SSL.htm
using System;
using System.IO;
using System.Net;

namespace Common.Ftp
{
    public class FtpClient
    {
        private readonly FtpConfig _ftpConfig;

        public FtpClient(FtpConfig ftpConfig)
        {
            _ftpConfig = ftpConfig;
            _ftpConfig.UserName = string.IsNullOrEmpty(_ftpConfig.UserName.Trim()) ? "anonymous" : _ftpConfig.UserName.Trim();
        }

        public byte[] DownloadFile(string remoteFilePath)
        {
            var requestUri = $"ftp://{_ftpConfig.HostIp}:{_ftpConfig.Port}/{remoteFilePath}";

            var ftpRequest = (FtpWebRequest)WebRequest.Create(requestUri);
            ftpRequest.Credentials = new NetworkCredential(_ftpConfig.UserName, _ftpConfig.Password);

            ftpRequest.Method = WebRequestMethods.Ftp.DownloadFile;
            ftpRequest.UseBinary = true;
            ftpRequest.EnableSsl = _ftpConfig.EnableSsl;

            ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, errors) => {
                return true;
            };

            using (var ftpResponse = (FtpWebResponse)ftpRequest.GetResponse())
            using (var ftpResponseStream = ftpResponse.GetResponseStream())
            using (var memoryStream = new MemoryStream())
            {
                ftpResponseStream?.CopyTo(memoryStream);
                return memoryStream.ToArray();
            }
        }

        public bool UploadFile (string ftpDirectory, FileInfo fileInfo)
        {
            var requestUri = $"ftp://{_ftpConfig.HostIp}:{_ftpConfig.Port}/{ftpDirectory}/{fileInfo.Name}";

            var ftpRequest = (FtpWebRequest)WebRequest.Create(requestUri);

            ftpRequest.Method = WebRequestMethods.Ftp.UploadFile;
            //ftpRequest.Credentials = new NetworkCredential(_ftpConfig.UserName, _ftpConfig.Password);
            ftpRequest.UseBinary = true;
            ftpRequest.KeepAlive = true;
            ftpRequest.EnableSsl = _ftpConfig.EnableSsl;

            ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, errors) =>
            {
                return true;
            };

            try
            {
                byte[] fileContents = new byte[fileInfo.Length];

                using (FileStream fr = fileInfo.OpenRead())
                {
                    fr.Read(fileContents, 0, Convert.ToInt32(fileInfo.Length));
                }

                ftpRequest.ContentLength = fileContents.Length;

                using (Stream requestStream = ftpRequest.GetRequestStream())
                {
                    requestStream.Write(fileContents, 0, fileContents.Length);
                }

                using (FtpWebResponse response = (FtpWebResponse)ftpRequest.GetResponse())
                {
                    //if (response.StatusCode == FtpStatusCode.CommandOK)
                        return true;
                    //else
                    //    throw new Exception(response.StatusDescription);
                }
            }
            catch (WebException e)
            {
                String status = ((FtpWebResponse)e.Response).StatusDescription;
            }

            return true;
        }
    }
}
