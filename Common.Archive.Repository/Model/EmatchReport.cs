using System;

namespace Common.ArchiveOrchestration.Repository.Model
{
    public class EmatchReport
    {
        public string Id { get; set; }
        public string CycleId { get; set; }
        public DateTime StatementDate { get; set; }
        public string FileName { get; set; }
        public string AccNum { get; set; }
        public string CisNum { get; set; }
        public string ProductName { get; set; }
        public string ProductDesc { get; set; }
        public string StatementType { get; set; }
        public string DeliveryMethod { get; set; }
        public string AccountNumber { get; set; }
        public string CisNumber { get; set; }
        public string CustomerName { get; set; }
        public string AddressLine1 { get; set; }
        public string AddressLine2 { get; set; }
        public string AddressLine3 { get; set; }
        public string Postcode { get; set; }
        public string State { get; set; }
        public string Dob { get; set; }
        public string BegBalance { get; set; }
        public string EndBalance { get; set; }
        public string EmailAddress { get; set; }
        public string DataMatch { get; set; }
        public string Remarks { get; set; }

        public string ForeignIndicator { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
    }
}
