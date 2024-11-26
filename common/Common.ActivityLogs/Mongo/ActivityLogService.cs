using Common.ActivityLogs.Model;
using Common.Config;
using Common.DataAccess;
using Common.DataAccess.Mongo;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Common.ActivityLogs.Mongo
{
    public class ActivityLogService: IActivityLogService
    {
        private readonly IRepository<ActivityLog> _activityLogsRepository;
        private const string ActivityLogsRepository = "ActivityLogs";

        public ActivityLogService(IOptions<DbConfig> dbConfig)
        {
            var databaseConfig = dbConfig.Value;

            var dbSettings = new DbSettings { ConnectionString = databaseConfig.ConnectionString, Database = databaseConfig.Database };

            _activityLogsRepository = new MongoRepository<ActivityLog>(dbSettings, ActivityLogsRepository);
        }

        public ActivityLogService(DbConfig dbConfig)
        {
            var databaseConfig = dbConfig;

            var dbSettings = new DbSettings { ConnectionString = databaseConfig.ConnectionString, Database = databaseConfig.Database };

            _activityLogsRepository = new MongoRepository<ActivityLog>(dbSettings, ActivityLogsRepository);
        }

        public async Task<ActivityLog> GetActivityLog(string activityLogId)
        {
            if (string.IsNullOrEmpty(activityLogId)) return null;

            return await _activityLogsRepository.GetByIdAsync(activityLogId);

        }

        public async Task<long> GetActivityLogsCount(SearchFilter searchFilter = null)
        {
            Expression<Func<ActivityLog, bool>> filterExpression = x => true;

            if (searchFilter != null && searchFilter.Filters.Count > 0)
                filterExpression = ExpressionBuilder.GetExpression<ActivityLog>(searchFilter);

            return await _activityLogsRepository.CountAsync(filterExpression);
        }

        public async Task<IEnumerable<ActivityLog>> GetActivityLogs(SearchFilter searchFilter = null, string orderBy = null, SortOrder sortOrder = SortOrder.Descending, int? page = null, int? pageSize = null)
        {
            Expression<Func<ActivityLog, bool>> filterExpression = x => true;

            if (searchFilter != null && searchFilter.Filters.Count > 0)
                filterExpression = ExpressionBuilder.GetExpression<ActivityLog>(searchFilter);

            IList<ActivityLog> activityLogs = null;

            if (page != null && pageSize != null)
                activityLogs = (await _activityLogsRepository.GetPaginatedAsync(filterExpression, orderBy, sortOrder, (int)page, (int)pageSize)).ToList();
            else
                activityLogs = (await _activityLogsRepository.GetAllAsync(filterExpression, null, orderBy, sortOrder)).ToList();

            return activityLogs;
        }

        public async Task<ActivityLog> LogActivity(ActivityLog activityLog)
        {
            return await _activityLogsRepository.UpdateOneAsync(activityLog);
        }

        public ActivityLog GetCommonActivityTrail(ActivityLog activityLog)
        {
            throw new NotImplementedException();
        }
    }
}
