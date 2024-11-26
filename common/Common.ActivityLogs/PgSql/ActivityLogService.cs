using Common.ActivityLogs.Model;
using Common.Config;
using Common.DataAccess;
using Common.DataAccess.PostgreSql;
using Common.DataAccess.RDBMS;
using Microsoft.Extensions.Options;
using NpgsqlTypes;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace Common.ActivityLogs.PgSql
{
    public class ActivityLogService : IActivityLogService
    {
        private readonly QueryHelper _queryHelper = null;

        public ActivityLogService(IOptions<DbConfig> dbConfig)
        {
            var databaseConfig = dbConfig.Value;

            _queryHelper = new QueryHelper(databaseConfig.ConnectionString);
        }

        public ActivityLogService(DbConfig dbConfig)
        {
            _queryHelper = new QueryHelper(dbConfig.ConnectionString);
        }

        public async Task<ActivityLog> GetActivityLog(string activityLogId)
        {
            if (string.IsNullOrEmpty(activityLogId)) return null;

            const string sql = @"SELECT * FROM ActivityLogs WHERE Id = @activityLogId";

            var parameters = new List<IDataParameter>
            {
                QueryHelper.CreateSqlParameter("@activityLogId", Guid.Parse(activityLogId), NpgsqlDbType.Uuid)
            };

            return (await _queryHelper.Read(sql, parameters, Make)).FirstOrDefault();

        }

        public async Task<long> GetActivityLogsCount(SearchFilter searchFilter = null)
        {
            string sql = @"SELECT COUNT(*) FROM ActivityLogs";

            if (searchFilter?.Filters?.Count() > 0)
            {
                sql += $" WHERE ({ExpressionBuilder.BuildWhereExpression(searchFilter)})";
            }

            return Convert.ToInt64(await _queryHelper.ExecuteScalar(sql, null));
        }

        public async Task<IEnumerable<ActivityLog>> GetActivityLogs(SearchFilter searchFilter = null, string orderBy = null, SortOrder sortOrder = SortOrder.Descending, int? page = null, int? pageSize = null)
        {
            string sql = @"SELECT * FROM ActivityLogs";

            if (searchFilter?.Filters?.Count() > 0)
            {
                sql += $" WHERE ({ExpressionBuilder.BuildWhereExpression(searchFilter)})";
            }

            if (!string.IsNullOrEmpty(orderBy))
            {
                var sort = sortOrder == SortOrder.Ascending ? "asc" : "desc";

                sql += $" ORDER BY {orderBy} {sort}";
            }

            if (page != null && pageSize != null)
            {
                if (string.IsNullOrEmpty(orderBy))
                    sql += $" ORDER BY Id";

                var offset = pageSize * (page - 1);

                sql += $" OFFSET {offset} ROWS FETCH NEXT {pageSize} ROWS ONLY";
            }

            var activityLogs = (await _queryHelper.Read(sql, null, Make)).ToList();

            return activityLogs;
        }

        public async Task<ActivityLog> LogActivity(ActivityLog activityLog)
        {
            const string sql = @"INSERT INTO ActivityLogs (
                               LoginId
                               ,SourceIP
                               ,WebServer
                               ,Application
                               ,Module
                               ,Action
                               ,ActivityOn
                               ,Browser
                               ,OtherInfo
                               ,CreatedBy
                               ,CreatedDate
                               ,UpdatedBy
                               ,UpdatedDate)
                            VALUES (
                                @loginId,
                                @sourceIP,
                                @webServer, 
                                @application,
                                @module,
                                @action,
                                @activityOn,
                                @browser,
                                @otherInfo,
                                @createdBy,
                                @createdDate,
                                @updatedBy,
                                @updatedDate) RETURNING Id;";

            _ = await _queryHelper.ExecuteScalar(sql, Take(activityLog));

            return activityLog;
        }

        private readonly Func<IDataReader, ActivityLog> Make = reader =>
            new ActivityLog
            {
                Id = reader["Id"].AsString(),
                LoginId = reader["LoginId"].AsString(),
                SourceIP = reader["SourceIP"].AsString(),
                WebServer = reader["WebServer"].AsString(),
                Application = reader["Application"].AsString(),
                Module = reader["Module"].AsString(),
                Action = reader["Action"].AsString(),
                ActivityOn = reader["ActivityOn"].AsDateTime(),
                Browser = reader["Browser"].AsString(),
                OtherInfo = reader["OtherInfo"].AsString(),
                CreatedBy = reader["CreatedBy"].AsString(),
                CreatedDate = reader.GetNullableDateTime("CreatedDate"),
                UpdatedBy = reader["UpdatedBy"].AsString(),
                UpdatedDate = reader.GetNullableDateTime("UpdatedDate")
            };

        private List<IDataParameter> Take(ActivityLog activityLog)
        {
            if (string.IsNullOrEmpty(activityLog.Id))
                activityLog.Id = Guid.NewGuid().ToString();

            var parameters = new List<IDataParameter>
            {
                QueryHelper.CreateSqlParameter("@activityLogId", new Guid(activityLog.Id), NpgsqlDbType.Uuid),
                QueryHelper.CreateSqlParameter("@loginId", activityLog.LoginId, NpgsqlDbType.Varchar),
                QueryHelper.CreateSqlParameter("@sourceIP", activityLog.SourceIP, NpgsqlDbType.Varchar),
                QueryHelper.CreateSqlParameter("@webServer", activityLog.WebServer, NpgsqlDbType.Varchar),
                QueryHelper.CreateSqlParameter("@application", activityLog.Application, NpgsqlDbType.Varchar),
                QueryHelper.CreateSqlParameter("@module", activityLog.Module, NpgsqlDbType.Varchar),
                QueryHelper.CreateSqlParameter("@action", activityLog.Action, NpgsqlDbType.Varchar),
                QueryHelper.CreateSqlParameter("@activityOn", activityLog.ActivityOn, NpgsqlDbType.Timestamp),
                QueryHelper.CreateSqlParameter("@browser", activityLog.Browser, NpgsqlDbType.Varchar),
                QueryHelper.CreateSqlParameter("@otherInfo", activityLog.OtherInfo, NpgsqlDbType.Varchar),
                QueryHelper.CreateSqlParameter("@createdBy", activityLog.CreatedBy, NpgsqlDbType.Varchar),
                QueryHelper.CreateSqlParameter("@createdDate", activityLog.CreatedDate, NpgsqlDbType.Timestamp),
                QueryHelper.CreateSqlParameter("@updatedBy", activityLog.UpdatedBy, NpgsqlDbType.Varchar),
                QueryHelper.CreateSqlParameter("@updatedDate", activityLog.UpdatedDate, NpgsqlDbType.Timestamp)
            };

            return parameters;
        }

        public ActivityLog GetCommonActivityTrail(ActivityLog activityLog)
        {
            throw new NotImplementedException();
        }
    }
}
