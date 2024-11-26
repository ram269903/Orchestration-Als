using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DSS.Als.Orchestration.Models
{
    public class MailSettings
    {
        public string ApiUrl { get; set; }
        public string SubjectFilePath { get; set; }
        public string BodyFilePath { get; set; }
    }
}
