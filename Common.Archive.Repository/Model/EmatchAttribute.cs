using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.ArchiveOrchestration.Repository.Model
{
    public class EmatchAttribute
    {
        public string Id { get; set; }
        public string CycleId { get; set; }
        public DateTime StatementDate { get; set; }
        public string FileName { get; set; }
        public string ProductName { get; set; }
        public string ProductDesc { get; set; }
        public string StatementType { get; set; }
        public string DeliveryMethod { get; set; }
        public string E1AccountNumber { get; set; }
        public string E2AccountNumber { get; set; }
        public string E1CisNumber { get; set; }
        public string E2CisNumber { get; set; }
        public string E1CustomerName { get; set; }
        public string E2CustomerName { get; set; }
        public string E1AddressLine1 { get; set; }
        public string E2AddressLine1 { get; set; }
        public string E1AddressLine2 { get; set; }
        public string E2AddressLine2 { get; set; }
        public string E1AddressLine3 { get; set; }
        public string E2AddressLine3 { get; set; }
        public string E1Postcode { get; set; }
        public string E2Postcode { get; set; }
        public string E1State { get; set; }
        public string E2State { get; set; }
        public string E1Dob { get; set; }
        public string E2Dob { get; set; }
        public string E1EmailAddress { get; set; }
        public string E2EmailAddress { get; set; }
        public string E1BegBalance { get; set; }
        public string E2BegBalance { get; set; }
        public string E1EndBalance { get; set; }
        public string E2EndBalance { get; set; }
        public string Status { get; set; }
        public string Remarks { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
        public string Extra1 { get; set; }
        public string Extra2 { get; set; }
        public string Extra3 { get; set; }
        public string Extra4 { get; set; }

    }
}
