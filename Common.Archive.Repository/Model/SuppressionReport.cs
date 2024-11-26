using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.ArchiveOrchestration.Repository.Model
{
    public class SuppressionReport
    {
        public string Id { get; set; }
        public DateTime StatementDate { get; set; }
        public string CycleID { get; set; }
        public string AccountNumber { get; set; }
        public string CISNumber { get; set; }
        public string ProductName { get; set; }
        public string ProductCode { get; set; }
        public string Dob { get; set; }
        public string EmailAddress { get; set; }
        public string DeliveryMethod { get; set; }
        public string FileName { get; set; }
        
        public string Remarks { get; set; }
        public string Status { get; set; }
        public string Description { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
    }
}
