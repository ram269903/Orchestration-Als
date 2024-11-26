using System;

namespace Common.Orchestration.Repository.Model
{
    public class EmatchArs
    {
        public string Id { get; set; }
        public string Cycle_Id { get; set; }
        public DateTime Statement_Date { get; set; }
        public string File_Name { get; set; }
        public string Product_Name { get; set; }
        public string statement_type { get; set; }
        public string Data_Match { get; set; }
        public string remarks { get; set; }
        public string created_date { get; set; }
        public string updated_date { get; set; }
    }
}
