
namespace Common.MicroBatch.Repository.Model
{
    public class GenerateLog
    {
        public string Id { get; set; }
        public DateTime StatementDate { get; set; }
        public string Name_1 { get; set; }
        public string Name_2 { get; set; }
        public string CycleID { get; set; }
        public string Address1 { get; set; }
        public string Address2 { get; set; }
        public string Address3 { get; set; }
        public string PostCode { get; set; }
        public string State { get; set; }
        public string AccountNumber { get; set; }
        public string CISNumber { get; set; }
        public string IdNumber { get; set; }
        public string ProductName { get; set; }
        public string ProductType { get; set; }
        public string ProductDescription { get; set; }
        public string LProductName { get; set; }
        public string BankCode { get; set; }
        public string SBankCode { get; set; }
        public string LBankCode { get; set; }
        public string DocumentType { get; set; }
        public string Staff { get; set; }
        public string LStaff { get; set; }
        public string DivisionCode { get; set; }
        public string Division { get; set; }
        public string LDivision { get; set; }
        public string Premier { get; set; }
        public string LPremier { get; set; }
        public string Entity { get; set; }
        public string LEntity { get; set; }
        public string Email { get; set; }
        public string DeliveryMethod { get; set; }
        public string LDeliveryMethod { get; set; }
        public string DOB { get; set; }
        public string LoanType { get; set; }
        public string LoanAmount { get; set; }
        public string BranchCode { get; set; }
        public string BranchName { get; set; }
        public string NoOfPages { get; set; }
        public string ForeignAddressIndicator { get; set; }
        public string BadIndicator { get; set; }

        //For Total original pages(PRT case)
        public string TotalPages { get; set; }
        public string ProductCode { get; set; }

        public string FileName { get; set; }

        public string ReportType { get; set; }
        public string DocId { get; set; }
        public string DocMasterId { get; set; }
        public string DocInstanceId { get; set; }
        public string PrintingVendorIndicator { get; set; }
        public string Remarks { get; set; }
        public bool IsDeleted { get; set; }
        public bool IsDisabled { get; set; }
        public string Status { get; set; }
        public string DocTypeId { get; set; }
        public string VendorId { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
        public string BatchType { get; set; }

        //ESDC accounts
        public string? ExtraAccount1 { get; set; }
        public string? ExtraAccount2 { get; set; }
        public string? ExtraAccount3 { get; set; }

    }
}
