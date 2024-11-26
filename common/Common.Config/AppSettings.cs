
using System.Collections.Generic;

namespace Common.Config
{
    public class AppSettings
    {
        public JwtToken JwtToken { get; set; }

        public bool IsLdapAuthentication { get; set; }

        //public LdapSettings LdapSettings { get; set; }

        public MailServer MailServer { get; set; }

        public string DocumentsStorage { get; set; }
        
		public string LicenseKeyLocation { get; set; }
        
		public Dictionary<string, string> OtherSettings { get; set; }
    }

    public class JwtToken
    {
        public string Secret { get; set; }

        public int TokenExpiryMinutes { get; set; }

    }

    public class MailServer
    {
        public string Host { get; set; }
        public int Port { get; set; }
        public string UserId { get; set; }
        public string Password { get; set; }
        public string SenderName { get; set; }
        public string SenderMailAddress { get; set; }
        public string SubjectPrefix { get; set; }
        public string MailBodayPath { get; set; }
        public bool UseSsl { get; set; }
        public bool IsAuthenticationRequired { get; set; }

    }

    //public class Doc1Settings
    //{
    //    public string Host { get; set; }
    //    public int Port { get; set; }
    //    public string UserId { get; set; }
    //    public string Password { get; set; }
    //    public string SenderName { get; set; }
    //    public string SenderMailAddress { get; set; }

    //    public bool UseSsl { get; set; }
    //    public bool IsAuthenticationRequired { get; set; }

    //}

    //public class License
    //{
    //    public string Secret { get; set; }

    //    public int TokenExpiryMinutes { get; set; }

    //}
}
