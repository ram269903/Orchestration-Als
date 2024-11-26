using Common.DataAccess;
using System;

namespace Common.ActivityLogs.Model
{
    public class ActivityLog : DbBaseEntity
    {
        public string LoginId { get; set; }
        public string SourceIP { get; set; }
        public string WebServer { get; set; }
        public string Application { get; set; }
        public string Module { get; set; }
        public string Action { get; set; }
        public DateTime ActivityOn { get; set; }
        public string Browser { get; set; }
        public string OtherInfo { get; set; }
    }
}
