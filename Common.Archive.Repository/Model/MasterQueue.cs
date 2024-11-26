using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.ArchiveOrchestration.Repository
{
    public class MasterQueue
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string JobId { get; set; }
        public string FilePath { get; set; }
        public int Priority { get; set; }
        public string Process { get; set; }
        public string Status { get; set; }
        public string Node { get; set; }
        public DateTime StatementDate { get; set; }
        public DateTime? CreatedDate { get; set; }

        public DateTime? UpdatedDate { get; set; }
    }

    public class SubQueue
    {
        public string? Id { get; set; }
        public string MainJobId { get; set; }
        public string? Name { get; set; }
        public string JobId { get; set; }
        public string FilePath { get; set; }
        public int Priority { get; set; }
        public string Process { get; set; }
        public string Status { get; set; }
        public string Node { get; set; }
        public DateTime StatementDate { get; set; }
        public DateTime? CreatedDate { get; set; }

        public DateTime? UpdatedDate { get; set; }
    }
}
