using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Communication.Model
{
    public class MailConfig
    {
        public string Host { get; set; }
        public int Port { get; set; }
        public string SenderName { get; set; }
        public string SenderMailAddress { get; set; }
        public string UserId { get; set; }
        public string Password { get; set; }
        public bool UseSsl { get; set; }
        public bool IsAuthenticationRequired { get; set; }
    }
}
