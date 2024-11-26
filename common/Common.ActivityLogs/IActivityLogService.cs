using Common.ActivityLogs.Model;
using Common.DataAccess;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Common.ActivityLogs
{
    public interface IActivityLogService
    {
        Task<ActivityLog> GetActivityLog(string activityLogId);
        Task<long> GetActivityLogsCount(SearchFilter searchFilter = null);
        Task<IEnumerable<ActivityLog>> GetActivityLogs(SearchFilter searchFilter = null, string orderBy = null, SortOrder sortOrder = SortOrder.Descending, int? page = null, int? pageSize = null);
        Task<ActivityLog> LogActivity(ActivityLog activityLog);

        ActivityLog GetCommonActivityTrail(ActivityLog activityLog);

    }
}
