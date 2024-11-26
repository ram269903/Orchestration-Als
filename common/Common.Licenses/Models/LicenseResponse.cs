using System;

namespace Common.Licenses.Models
{
    public class LicenseResponse
    {
        public string LicensedTo { get; set; }
        public DateTime ValidFrom { get; set; }
        public DateTime ValidTo { get; set; }
        public string FreeText { get; set; }
        public long ActiveUsers { get; set; }
        //public string Status { get; set; }
        //public string Message { get; set; }
    }
}
