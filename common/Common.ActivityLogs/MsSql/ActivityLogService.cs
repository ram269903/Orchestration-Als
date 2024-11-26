using Common.ActivityLogs.Model;
using Common.Config;
using Common.DataAccess;
using Common.DataAccess.MsSql;
using Common.DataAccess.RDBMS;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Common.ActivityLogs.MsSql
{
    public class ActivityLogService : IActivityLogService
    {
        private readonly QueryHelper _queryHelper = null;
        //private readonly IGenericRepository<ActivityLog> _activityLogsRepository;

        public ActivityLogService(IOptions<DbConfig> dbConfig)
        {
            var databaseConfig = dbConfig.Value;

            _queryHelper = new QueryHelper(databaseConfig.ConnectionString);
        }

        public ActivityLogService(DbConfig dbConfig)
        {
            _queryHelper = new QueryHelper(dbConfig.ConnectionString);
        }

        public ActivityLog GetCommonActivityTrail(ActivityLog auditTrail)
        {

            auditTrail.Application = "Back Office";

            //using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
            //{
            //    socket.Connect("8.8.8.8", 65530);
            //    IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
            //    auditTrail.SourceIP = endPoint.Address.ToString();
            //}
            var host = Dns.GetHostName();
            IPHostEntry ipEntry = Dns.GetHostEntry(host);
            IPAddress[] addr = ipEntry.AddressList;
            auditTrail.SourceIP = addr[addr.Length - 1].ToString();
            auditTrail.WebServer = host;
            auditTrail.CreatedDate = DateTime.Now;
            auditTrail.ActivityOn = DateTime.Now;

            return auditTrail;
        }
        public async Task<ActivityLog> GetActivityLog(string activityLogId)
        {
            if (string.IsNullOrEmpty(activityLogId)) return null;

            const string sql = @"SELECT * FROM [dbo].[ActivityLogs] WHERE [Id] = @activityLogId";

            var parameters = new List<IDataParameter>
            {
                QueryHelper.CreateSqlParameter("@activityLogId", Guid.Parse(activityLogId), SqlDbType.UniqueIdentifier)
            };

            return (await _queryHelper.Read(sql, parameters, Make)).FirstOrDefault();

        }

        public async Task<long> GetActivityLogsCount(SearchFilter searchFilter = null)
        {
            string sql = @"SELECT COUNT(*) FROM [dbo].[ActivityLogs]";

            if (searchFilter?.Filters?.Count() > 0)
            {
                sql += $" WHERE ({ExpressionBuilder.BuildWhereExpression(searchFilter)})";
            }

            return Convert.ToInt64(await _queryHelper.ExecuteScalar(sql, null));
        }

        public async Task<IEnumerable<ActivityLog>> GetActivityLogs(SearchFilter searchFilter = null, string orderBy = null, SortOrder sortOrder = SortOrder.Descending, int? page = null, int? pageSize = null)
        {
            string sql = @"SELECT * FROM [dbo].[ActivityLogs]";

            if (searchFilter?.Filters?.Count() > 0)
            {
                sql += $" WHERE ({ExpressionBuilder.BuildWhereExpression(searchFilter)})";
            }

            if (!string.IsNullOrEmpty(orderBy))
            {
                var sort = sortOrder == SortOrder.Ascending ? "asc" : "desc";

                sql += $" ORDER BY [{orderBy}] {sort}";
            }

            if (page != null && pageSize != null)
            {
                if (string.IsNullOrEmpty(orderBy))
                    sql += $" ORDER BY [Id]";

                var offset = pageSize * (page - 1);

                sql += $" OFFSET {offset} ROWS FETCH NEXT {pageSize} ROWS ONLY";
            }

            var activityLogs = (await _queryHelper.Read(sql, null, Make)).ToList();

            return activityLogs;
        }

        public async Task<ActivityLog> LogActivity(ActivityLog activityLog)
        {
            const string sql = @"INSERT [dbo].[ActivityLogs] (
                               [LoginId]
                               ,[SourceIP]
                               ,[WebServer]
                               ,[Application]
                               ,[Module]
                               ,[Action]
                               ,[ActivityOn]
                               ,[Browser]
                               ,[OtherInfo]
                               ,[CreatedBy]
                               ,[CreatedDate]
                               ,[UpdatedBy]
                               ,[UpdatedDate])
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
                                @updatedDate)";

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
                QueryHelper.CreateSqlParameter("@activityLogId", new Guid(activityLog.Id), SqlDbType.UniqueIdentifier),
                QueryHelper.CreateSqlParameter("@loginId", activityLog.LoginId, SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@sourceIP", activityLog.SourceIP, SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@webServer", activityLog.WebServer, SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@application", activityLog.Application, SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@module", activityLog.Module, SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@action", activityLog.Action, SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@activityOn", activityLog.ActivityOn, SqlDbType.DateTime2),
                QueryHelper.CreateSqlParameter("@browser", activityLog.Browser, SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@otherInfo", activityLog.OtherInfo, SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@createdBy", activityLog.CreatedBy, SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@createdDate", activityLog.CreatedDate, SqlDbType.DateTime2),
                QueryHelper.CreateSqlParameter("@updatedBy", activityLog.UpdatedBy, SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@updatedDate", activityLog.UpdatedDate, SqlDbType.DateTime2)
            };

            return parameters;
        }
    }
}
