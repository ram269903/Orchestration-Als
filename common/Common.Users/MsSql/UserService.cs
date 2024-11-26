using Common.Config;
using Common.DataAccess;
using Common.DataAccess.MsSql;
using Common.DataAccess.RDBMS;
using Common.Users.Models;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace Common.Users.MsSql
{
    public class UserService : IUserService
    {
        private readonly QueryHelper _queryHelper = null;

        public UserService(IOptions<DbConfig> dbConfig)
        {
            var databaseConfig = dbConfig.Value;

            _queryHelper = new QueryHelper(databaseConfig.ConnectionString);
        }

        public UserService(DbConfig dbConfig)
        {
            _queryHelper = new QueryHelper(dbConfig.ConnectionString);
        }

        public async void ResetLoginStatus()
        {

            const string sql = "UPDATE [dbo].[Users] SET [IsLogedIn] = 0 WHERE [IsLogedIn] = 1";
            
            _ = await _queryHelper.ExecuteNonQuery(sql, null);

        }

        public async Task<User> GetUserByLoginId(string loginId)
        {
            if (string.IsNullOrEmpty(loginId)) return null;

            const string sql = @"SELECT * FROM [dbo].[Users] WHERE lower([LoginId]) = @loginId AND [IsDeleted] = 0";

            var parameters = new List<IDataParameter>
            {
                QueryHelper.CreateSqlParameter("@loginId", loginId.ToLower(), SqlDbType.NVarChar)
            };

            var user = (await _queryHelper.Read(sql, parameters, Make)).FirstOrDefault();

            if (user == null) return null;

            user.Groups = (await GetGroups(user.Id)).ToList();

            return user;
        }
        public async Task<bool> IsDelete(string userId)
        {
            const string sql = @"SELECT COUNT (*) FROM Users WHERE LOGINID = @userId AND IsDeleted = 1";

            var parameters = new List<IDataParameter>
            {
                QueryHelper.CreateSqlParameter("@userId", userId, SqlDbType.NVarChar)
            };

            var count = Convert.ToInt64(await _queryHelper.ExecuteScalar(sql,parameters));

            if (count > 0)
                return true;
            else
                return false;

            
        }

        public async Task<User> GetUserByToken(string token)
        {
            if (string.IsNullOrEmpty(token)) return null;

            const string sql = @"SELECT * FROM [dbo].[Users] WHERE [Token] = @token AND [IsDeleted] = 0";

            var parameters = new List<IDataParameter>
            {
                QueryHelper.CreateSqlParameter("@token", token, SqlDbType.NVarChar)
            };

            var user = (await _queryHelper.Read(sql, parameters, Make)).FirstOrDefault();

            if (user == null) return null;

            user.Groups = (await GetGroups(user.Id)).ToList();

            return user;
        }

        public async Task<User> GetUser(string userId)
        {
            const string sql = @"SELECT * FROM [dbo].[Users] WHERE [Id] = @userId AND [IsDeleted] = 0";

            var parameters = new List<IDataParameter>
            {
                QueryHelper.CreateSqlParameter("@userId", Guid.Parse(userId), SqlDbType.UniqueIdentifier)
            };

            var user = (await _queryHelper.Read(sql, parameters, Make))?.FirstOrDefault();

            if (user == null) return null;

            user.Groups = (await GetGroups(user.Id))?.ToList();

            return user;
        }

        public async Task<long> GetUsersCount(SearchFilter searchFilter = null)
        {
            string sql = @"SELECT COUNT(*) FROM [dbo].[Users]";

            if (searchFilter?.Filters?.Count() > 0)
            {
                sql += $" WHERE ({ExpressionBuilder.BuildWhereExpression(searchFilter)}) AND [IsDeleted] = 0";
            }
            else
                sql += $" WHERE [IsDeleted] = 0";

            return Convert.ToInt64(await _queryHelper.ExecuteScalar(sql, null));
        }

        public async Task<long> GetUserGroupCount(SearchFilter searchFilter = null)
        {
            string sql = @"SELECT COUNT(*) FROM User_Access";

            if (searchFilter?.Filters?.Count() > 0)
            {
                sql += $" WHERE ({ExpressionBuilder.BuildWhereExpression(searchFilter)})";
            }

            return Convert.ToInt64(await _queryHelper.ExecuteScalar(sql, null));

        }

        public async Task<IEnumerable<User>> GetUsers(SearchFilter searchFilter = null, string orderBy = null, SortOrder sortOrder = SortOrder.Descending, int? page = null, int? pageSize = null, string[] fields = null)
        {
            var columns = "*";

            if (fields != null && fields.Length > 0)
                columns = string.Join(",", fields);

            string sql = $"SELECT {columns} FROM [dbo].[Users]";
            if (!string.IsNullOrEmpty(orderBy))
            {
                if ("roleid".ToLower().Equals(orderBy.ToLower()))
                {
                    sql = $"SELECT {columns} FROM [dbo].[Users] join Roles on Users.RoleId=Roles.Id ";
                    //"ORDER BY Name asc";
                }
            }

            if (searchFilter?.Filters?.Count() > 0)
            {
                sql += $" WHERE ({ExpressionBuilder.BuildWhereExpression(searchFilter)}) AND Users.[IsDeleted] = 0";
            }
            else
                sql += $" WHERE Users.[IsDeleted] = 0";

            if (!string.IsNullOrEmpty(orderBy))
            {
                if ("roleid".ToLower().Equals(orderBy.ToLower()))
                    orderBy = "name";
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

            var users = (await _queryHelper.Read(sql, null, Make)).ToList();

            foreach (var user in users)
            {
                user.Groups = (await GetGroups(user.Id)).ToList();
            }
            
            return users;
        }

        private async Task<IEnumerable<string>> GetGroups(string userId)
        {
            const string sql = @"SELECT * FROM [dbo].[User_Access] WHERE [UserId] = @userId";

            var parameters = new List<IDataParameter>
            {
                QueryHelper.CreateSqlParameter("@userId", userId, SqlDbType.NVarChar)
            };

            var groups = await _queryHelper.Read(sql, parameters, MakeGroupIds);

            return groups.Distinct();
        }

        public async Task<string> CheckLoginIdExists(string loginId)
        {
            var user = await GetUserByLoginId(loginId);

            return user?.Id;
        }

        public async Task<User> SaveUser(User user)
        {
            if (string.IsNullOrEmpty(user.Id))
                return await InsertUser(user);
            else
                return await UpdateUser(user);
        }

        public async void UpdateIsLogedIn(User user)
        {
            const string sql = @"UPDATE [dbo].[Users]
                                SET 
                                    [IsLogedIn] = @isLogedIn
                               WHERE 
                                    [Id] = @userId";

            _ = await _queryHelper.ExecuteNonQuery(sql, Take(user));

        }

        public async void UpdateLastLogin(User user)
        {
            const string sql = @"UPDATE [dbo].[Users]
                                SET 
                                    [LastLogin] = @lastLogin,
                                    [Token] = @token,
                                    [IsLogedIn] = @isLogedIn
                               WHERE 
                                    [Id] = @userId";

            _ = await _queryHelper.ExecuteNonQuery(sql, Take(user));
        }

        public async void UpdateLoginUserCount()
        {
            var sql = "SELECT COUNT(*) FROM [dbo].[Users] WHERE [IsLogedIn] = 1";

            var loginCount = await _queryHelper.ExecuteNonQuery(sql, null);

            //var loginTracker = await _loginTrackerRepository.GetOneAsync(x => x.Date == DateTime.Now.Date);

            //if (loginTracker == null)
            //    await _loginTrackerRepository.UpdateOneAsync(new LoginTracker { Date = DateTime.Now.Date, MaxLoginUsers = loginCount });
            //else
            //{
            //    if (loginCount > loginTracker.MaxLoginUsers)
            //    {
            //        loginTracker.MaxLoginUsers = loginCount;
            //        await _loginTrackerRepository.UpdateOneAsync(loginTracker);
            //    }
            //}
        }

        public async Task<bool> DeleteUserByLoginId(string loginId)
        {
            const string sql = "DELETE FROM [dbo].[Users] WHERE [LoginId] = @loginId";
            //const string sql = @"UPDATE [dbo].[Users] SET [IsDeleted] = 1 WHERE [LoginId] = @loginId";

            var parameters = new List<IDataParameter>
            {
                QueryHelper.CreateSqlParameter("@loginId", loginId, SqlDbType.NVarChar)
            };

            return await _queryHelper.ExecuteNonQuery(sql, parameters) == 1;
        }

        public async Task<bool> DeleteUserFlag(string userId)
        {
           // const string sql = "DELETE FROM [dbo].[Users] WHERE [Id] = @userId";
            const string sql = @"UPDATE [dbo].[Users] SET [IsDeleted] = 1 WHERE [Id] = @userId";

            var parameters = new List<IDataParameter>
            {
                QueryHelper.CreateSqlParameter("@userId", Guid.Parse(userId), SqlDbType.UniqueIdentifier)
            };

            return await _queryHelper.ExecuteNonQuery(sql, parameters) == 1;
        }
        public async Task<bool> DeleteUser(string userId)
        {
            const string sql = "DELETE FROM [dbo].[Users] WHERE [Id] = @userId";
            //const string sql = @"UPDATE [dbo].[Users] SET [IsDeleted] = 1 WHERE [Id] = @userId";

            var parameters = new List<IDataParameter>
            {
                QueryHelper.CreateSqlParameter("@userId", Guid.Parse(userId), SqlDbType.UniqueIdentifier)
            };

            return await _queryHelper.ExecuteNonQuery(sql, parameters) == 1;
        }

        private async Task<User> InsertUser(User user)
        {
            const string sql = @"INSERT [dbo].[Users] (
                                [LoginId],
                                [Password],
                                [FirstName], 
                                [LastName],
                                [EmailId],
                                [Department],
                                [PhoneNumber],
                                [RoleId],
                                [IsActive],
                                [LastLogin],
                                [IsLogedIn],
                                [IsLdap],
                                [Token],
                                [CreatedBy],
                                [CreatedDate],
                                [UpdatedBy],
                                [UpdatedDate],
                                [IsDeleted],
                                [StatusUpdatedDate])
                            OUTPUT Inserted.ID
                            VALUES (
                                @loginId,
                                @password,
                                @firstName, 
                                @lastName,
                                @emailId,
                                @department,
                                @phoneNumber,
                                @roleId,
                                @isActive,
                                @lastLogin,
                                @isLogedIn,
                                @isLdap,
                                @token,
                                @createdBy,
                                @createdDate,
                                @updatedBy,
                                @updatedDate,
                                @isDeleted,
                                @StatusUpdatedDate)";

            var id = await _queryHelper.ExecuteScalar(sql, Take(user));

            user.Id = id.ToString();

            if (user.Groups != null && user.Groups.Count() > 0)
               await UpdateUserAccess(user);

            return user;
        }

        private async Task<User> UpdateUser(User user)
        {
          
            const string sql = @"UPDATE [dbo].[Users]
                                SET 
                                    [LoginId] = @loginId,
                                    [Password] = @password,
                                    [FirstName] = @firstName,
                                    [LastName] = @lastName,
                                    [EmailId] = @emailId,
                                    [Department] = @department,
                                    [PhoneNumber] = @phoneNumber,
                                    [RoleId] = @roleId,
                                    [IsActive] = @isActive, 
                                    [LastLogin] = @lastLogin,
                                    [IsLogedIn] = @isLogedIn,
                                    [IsLdap] = @isLdap,
                                    [Token] = @token,
                                    [CreatedBy] = @createdBy,
                                    [CreatedDate] = @createdDate,
                                    [UpdatedBy] = @updatedBy,
                                    [UpdatedDate] = @updatedDate,
                                    [IsDeleted] = @isDeleted,
                                    [StatusUpdatedDate]= @statusUpdatedDate
                                    
                               WHERE 
                                    [Id] = @userId";

            _ = await _queryHelper.ExecuteNonQuery(sql, Take(user));

            if (user.Groups != null)
                await UpdateUserAccess(user);

            return user;
        }

        private async Task UpdateUserAccess(User user)
        {
            const string deleteSql = @"DELETE FROM [dbo].[User_Access] WHERE [UserId] = @userId";

            var parameters = new List<IDataParameter>
            {
                QueryHelper.CreateSqlParameter("@userId", user.Id, SqlDbType.NVarChar)
            };
            
            _ = await _queryHelper.ExecuteNonQuery(deleteSql, parameters);

            var values = "";
            
            foreach (var groupId in user.Groups.Distinct())
            {
                values += $"('{user.Id}', '{groupId}'),";
            }

            if (!string.IsNullOrEmpty(values))
            {
                values = values.Remove(values.Length - 1);

                //_ = await _queryHelper.ExecuteQuery($"DELETE FROM [dbo].[User_Access] WHERE [UserId] = '{user.Id}'", null);

                var sql = $@"INSERT [dbo].[User_Access] ([UserId], [GroupId]) VALUES {values}";

                var status = await _queryHelper.ExecuteQuery(sql, null);
            }

        }

        private readonly Func<IDataReader, User> Make = reader =>
            new User
            {
                Id = reader["Id"].AsString(),
                FirstName = reader["FirstName"].AsString(),
                LastName = reader["LastName"].AsString(),
                LoginId = reader["LoginId"].AsString(),
                Password = reader["Password"]?.AsString(),
                EmailId = reader["EmailId"].AsString(),
                Department = reader["Department"].AsString(),
                PhoneNumber = reader["PhoneNumber"].AsString(),
                RoleId = reader["RoleId"].AsString(),
                IsActive = reader["IsActive"].AsBool(),
                IsSuperUser = reader["IsSuperUser"].AsBool(),
                LastLogin = reader.GetNullableDateTime("LastLogin"),
                IsLogedIn = reader["IsLogedIn"].AsBool(),
                IsLdap = reader["IsLdap"]?.AsBool(),
                Token = reader["Token"].AsString(),
                IsDeleted = reader["IsDeleted"].AsBool(),
                CreatedBy = reader["CreatedBy"].AsString(),
                CreatedDate = reader.GetNullableDateTime("CreatedDate"),
                UpdatedBy = reader["UpdatedBy"].AsString(),
                UpdatedDate = reader.GetNullableDateTime("UpdatedDate"),
                StatusUpdatedDate= reader.GetNullableDateTime("StatusUpdatedDate"),
                
            };

        private List<IDataParameter> Take(User user)
        {
            if (string.IsNullOrEmpty(user.Id))
                user.Id = Guid.NewGuid().ToString();

            var parameters = new List<IDataParameter>
            {
                QueryHelper.CreateSqlParameter("@userId", new Guid(user.Id), SqlDbType.UniqueIdentifier),
                QueryHelper.CreateSqlParameter("@firstName", user.FirstName, SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@lastName", user.LastName, SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@loginId", user.LoginId, SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@password", user.Password, SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@emailId", user.EmailId, SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@department", user.Department, SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@phoneNumber", user.PhoneNumber, SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@roleId", new Guid(user.RoleId), SqlDbType.UniqueIdentifier),
                QueryHelper.CreateSqlParameter("@isActive", user.IsActive == true? 1: 0, SqlDbType.Bit),
                QueryHelper.CreateSqlParameter("@isSuperUser", user.IsSuperUser == true? 1: 0, SqlDbType.Bit),
                QueryHelper.CreateSqlParameter("@lastLogin", user.LastLogin, SqlDbType.DateTime2),
                QueryHelper.CreateSqlParameter("@isLogedIn", user.IsLogedIn == true? 1: 0, SqlDbType.Bit),
                QueryHelper.CreateSqlParameter("@isLdap", user.IsLdap == true? 1: 0, SqlDbType.Bit),
                QueryHelper.CreateSqlParameter("@token", user.Token, SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@isDeleted", user.IsDeleted == true? 1: 0, SqlDbType.Bit),
                QueryHelper.CreateSqlParameter("@createdBy", user.CreatedBy, SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@createdDate", user.CreatedDate, SqlDbType.DateTime2),
                QueryHelper.CreateSqlParameter("@updatedBy", user.UpdatedBy, SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@updatedDate", user.UpdatedDate, SqlDbType.DateTime2),
                QueryHelper.CreateSqlParameter("@statusUpdatedDate",user.StatusUpdatedDate,SqlDbType.DateTime),
            
            };

            return parameters;
        }

        public async Task<long> GetActiveUsersCount()
        {
            string sql = @"SELECT COUNT(*) FROM [dbo].[Users] WHERE [IsLogedIn] = 1";

            return Convert.ToInt64(await _queryHelper.ExecuteScalar(sql, null));
        }

        private readonly Func<IDataReader, string> MakeGroupIds = reader => reader["GroupId"].AsString();

        public async Task<IEnumerable<User>> GetOrphanAccounts(SearchFilter searchFilter = null, string orderBy = null, SortOrder sortOrder = SortOrder.Descending, int? page = null, int? pageSize = null, string[] fields = null)
        {
            var columns = "*";//"Id,Password,LoginId,FirstName,LastName,LastLogin,IsActive";

            if (fields != null && fields.Length > 0)
                columns = string.Join(",", fields);

            string sql = $"SELECT {columns} FROM [dbo].[Users]";

            if (searchFilter?.Filters?.Count() > 0)
            {
                sql += $" WHERE ({ExpressionBuilder.BuildWhereExpression(searchFilter)}) AND  ([IsActive]='0' AND [IsDeleted] = 0) ";
            }
            else
                sql += $" WHERE ([IsActive]='0' AND [IsDeleted] = 0 )";

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

            var users = (await _queryHelper.Read(sql, null, Make)).ToList();


            return users;
        }


        public async Task<long> GetOrphanAccountsCount(SearchFilter searchFilter = null)
        {
            //var columns = "*";
            string sql = $"SELECT COUNT(*) FROM [dbo].[Users]";

            if (searchFilter?.Filters?.Count() > 0)
            {
                sql += $" WHERE ({ExpressionBuilder.BuildWhereExpression(searchFilter)}) AND ( [LoginId]  NOT LIKE 'a%' AND [LoginId] NOT LIKE 's%' AND [IsDeleted] = 0 ) or ([IsSuperUSer] = 1)";
            }
            else
                sql += $" WHERE ([LoginId]  NOT LIKE 'a%' AND [LoginId] NOT LIKE 's%' AND [IsDeleted] = 0 ) or ([IsSuperUSer] = 1) ";

            return Convert.ToInt64(await _queryHelper.ExecuteScalar(sql, null));
        }

        public async Task<DateTime> GetLastActionTime(string loginId)
        {
            // string sql = $"select top 1 ActivityOn from ActivityLogs where loginId='{loginId.ToLower()}' order by ActivityOn desc";
            string sql = $"select LastActionTime from Users where LoginId='{loginId.ToLower()}'";
            return Convert.ToDateTime(await _queryHelper.ExecuteScalar(sql, null));
        }

        public async Task<DateTime> UpdateLastActionTime(string loginId)
        {
            string sql = $"update users set LastActionTime='{DateTime.Now.ToString("MM-dd-yyyy HH:mm:ss")}' where LoginId='{loginId.ToLower()}'";
            return Convert.ToDateTime(await _queryHelper.ExecuteScalar(sql, null));
        }

        public async Task<IEnumerable<User>> GetAccountsMorethen30days()
        {
            const string sql = $"select * from (SELECT *, CASE WHEN LastLogin IS NOT NULL THEN LastLogin WHEN LastLogin is null THEN createddate END AS logintime FROM users ) innertable where DATEDIFF(day, logintime, GETDATE()) > 30";
           
            var users = (await _queryHelper.Read(sql, null, Make)).ToList();

            return users;
        }
        public async Task<IEnumerable<User>> GetAccountsMorethen90days()
        {
            const string sql = $"select * from (SELECT *, CASE WHEN LastLogin IS NOT NULL THEN LastLogin WHEN LastLogin is null THEN createddate END AS logintime FROM users ) innertable where DATEDIFF(day, logintime, GETDATE()) > 90";

            var users = (await _queryHelper.Read(sql, null, Make)).ToList();

            return users;
        }
    }
}
