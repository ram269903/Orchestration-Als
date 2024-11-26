using Common.Config;
using Common.DataAccess;
using Common.DataAccess.PostgreSql;
using Common.DataAccess.RDBMS;
using Common.Users.Models;
using Microsoft.Extensions.Options;
using NpgsqlTypes;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace Common.Users.PgSql
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

            const string sql = "UPDATE Users SET IsLogedIn = false WHERE IsLogedIn = true";
            
            _ = await _queryHelper.ExecuteNonQuery(sql, null);

        }

        public async Task<User> GetUserByLoginId(string loginId)
        {
            if (string.IsNullOrEmpty(loginId)) return null;

            const string sql = @"SELECT * FROM Users WHERE lower(LoginId) = @loginId";

            var parameters = new List<IDataParameter>
            {
                QueryHelper.CreateSqlParameter("@loginId", loginId.ToLower(), NpgsqlDbType.Varchar)
            };

            var user = (await _queryHelper.Read(sql, parameters, Make)).FirstOrDefault();

            if (user == null) return null;

            user.Groups = (await GetGroups(user.Id)).ToList();

            return user;
        }
        public async Task<bool> DeleteUserFlag(string userId)
        {
            // const string sql = "DELETE FROM [dbo].[Users] WHERE [Id] = @userId";
            const string sql = @"UPDATE [dbo].[Users] SET [IsDeleted] = 1 WHERE [Id] = @userId";

            var parameters = new List<IDataParameter>
            {
                QueryHelper.CreateSqlParameter("@userId", Guid.Parse(userId), NpgsqlDbType.Uuid)
            };

            return await _queryHelper.ExecuteNonQuery(sql, parameters) == 1;
        }

        public async Task<User> GetUserByToken(string token)
        {
            if (string.IsNullOrEmpty(token)) return null;

            const string sql = @"SELECT * FROM Users WHERE Token = @token";

            var parameters = new List<IDataParameter>
            {
                QueryHelper.CreateSqlParameter("@token", token, NpgsqlDbType.Varchar)
            };

            var user = (await _queryHelper.Read(sql, parameters, Make)).FirstOrDefault();

            if (user == null) return null;

            user.Groups = (await GetGroups(user.Id)).ToList();

            return user;
        }
        
        public async Task<bool> IsDelete(string userId)
        {
            const string sql = @"SELECT * FROM Users WHERE LOGINID = @userId AND IsDeleted = 1";

            var parameters = new List<IDataParameter>
            {
                QueryHelper.CreateSqlParameter("@userId", userId, NpgsqlDbType.Varchar)
            };

             var count= Convert.ToInt64(await _queryHelper.ExecuteScalar(sql,parameters));

            if (count > 0)
                return true;
            else
               return false;

           return false;
        }
        public async Task<User> GetUser(string userId)
        {
            const string sql = @"SELECT * FROM Users WHERE Id = @userId AND IsDeleted = false";

            var parameters = new List<IDataParameter>
            {
                QueryHelper.CreateSqlParameter("@userId", Guid.Parse(userId), NpgsqlDbType.Uuid)
            };

            var user = (await _queryHelper.Read(sql, parameters, Make)).FirstOrDefault();

            user.Groups = (await GetGroups(user.Id)).ToList();

            return user;
        }

        public async Task<long> GetUsersCount(SearchFilter searchFilter = null)
        {
            string sql = @"SELECT COUNT(*) FROM Users";

            if (searchFilter?.Filters?.Count() > 0)
            {
                sql += $" WHERE ({ExpressionBuilder.BuildWhereExpression(searchFilter)}) AND IsDeleted = false";
            }
            else
                sql += $" WHERE IsDeleted = false";

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

            string sql = $"SELECT {columns} FROM Users";

            if (searchFilter?.Filters?.Count() > 0)
            {
                sql += $" WHERE ({ExpressionBuilder.BuildWhereExpression(searchFilter)}) AND IsDeleted = false";
            }
            else
                sql += $" WHERE IsDeleted = false";

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

            var users = (await _queryHelper.Read(sql, null, Make)).ToList();

            foreach (var user in users)
            {
                user.Groups = (await GetGroups(user.Id)).ToList();
            }
            
            return users;
        }

        private async Task<IEnumerable<string>> GetGroups(string userId)
        {
            const string sql = @"SELECT * FROM User_Access WHERE UserId = @userId";

            var parameters = new List<IDataParameter>
            {
                QueryHelper.CreateSqlParameter("@userId", Guid.Parse(userId), NpgsqlDbType.Uuid)
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
            const string sql = @"UPDATE Users
                                SET 
                                    IsLogedIn = @isLogedIn
                               WHERE 
                                    Id = @userId";

            _ = await _queryHelper.ExecuteNonQuery(sql, Take(user));

        }

        public async void UpdateLastLogin(User user)
        {
            const string sql = @"UPDATE Users
                                SET 
                                    LastLogin = @lastLogin,
                                    Token = @token,
                                    IsLogedIn = @isLogedIn
                               WHERE 
                                    Id = @userId";

            _ = await _queryHelper.ExecuteNonQuery(sql, Take(user));
        }

        public async void UpdateLoginUserCount()
        {
            var sql = "SELECT COUNT(*) FROM Users WHERE IsLogedIn = true";

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
            const string sql = "DELETE FROM Users WHERE LoginId = @loginId";

            var parameters = new List<IDataParameter>
            {
                QueryHelper.CreateSqlParameter("@loginId", loginId, NpgsqlDbType.Varchar)
            };

            return await _queryHelper.ExecuteNonQuery(sql, parameters) == 1;
        }

        public async Task<bool> DeleteUser(string userId)
        {
            const string sql = "DELETE FROM Users WHERE Id = @userId";

            var parameters = new List<IDataParameter>
            {
                QueryHelper.CreateSqlParameter("@userId", Guid.Parse(userId), NpgsqlDbType.Uuid)
            };

            return await _queryHelper.ExecuteNonQuery(sql, parameters) == 1;
        }

        private async Task<User> InsertUser(User user)
        {
            const string sql = @"INSERT INTO Users (
                                LoginId,
                                Password,
                                FirstName, 
                                LastName,
                                EmailId,
                                PhoneNumber,
                                RoleId,
                                IsActive,
                                LastLogin,
                                IsLogedIn,
                                Token,
                                CreatedBy,
                                CreatedDate,
                                UpdatedBy,
                                UpdatedDate,
                                IsDeleted)
                            VALUES (
                                @loginId,
                                @password,
                                @firstName, 
                                @lastName,
                                @emailId,
                                @phoneNumber,
                                @roleId,
                                @isActive,
                                @lastLogin,
                                @isLogedIn,
                                @token,
                                @createdBy,
                                @createdDate,
                                @updatedBy,
                                @updatedDate,
                                @isDeleted) RETURNING Id;";

            var id = await _queryHelper.ExecuteScalar(sql, Take(user));

            user.Id = id.ToString();

            if (user.Groups != null && user.Groups.Count() > 0)
               await UpdateUserAccess(user);

            return user;
        }

        private async Task<User> UpdateUser(User user)
        {
          
            const string sql = @"UPDATE Users
                                SET 
                                    LoginId = @loginId,
                                    Password = @password,
                                    FirstName = @firstName,
                                    LastName = @lastName,
                                    EmailId = @emailId,
                                    PhoneNumber = @phoneNumber,
                                    RoleId = @roleId,
                                    IsActive = @isActive, 
                                    LastLogin = @lastLogin,
                                    IsLogedIn = @isLogedIn,
                                    Token = @token,
                                    CreatedBy = @createdBy,
                                    CreatedDate = @createdDate,
                                    UpdatedBy = @updatedBy,
                                    UpdatedDate = @updatedDate,
                                    IsDeleted = @isDeleted
                               WHERE 
                                    Id = @userId";

            _ = await _queryHelper.ExecuteNonQuery(sql, Take(user));

            if (user.Groups != null)
                await UpdateUserAccess(user);

            return user;
        }

        private async Task UpdateUserAccess(User user)
        {
            const string deleteSql = @"DELETE FROM User_Access WHERE UserId = @userId";

            var parameters = new List<IDataParameter>
            {
                QueryHelper.CreateSqlParameter("@userId", Guid.Parse(user.Id), NpgsqlDbType.Uuid)
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

                var sql = $@"INSERT INTO User_Access (UserId, GroupId) VALUES {values}";

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
                Password = reader["Password"].AsString(),
                EmailId = reader["EmailId"].AsString(),
                PhoneNumber = reader["PhoneNumber"].AsString(),
                RoleId = reader["RoleId"].AsString(),
                IsActive = reader["IsActive"].AsBool(),
                IsSuperUser = reader["IsSuperUser"].AsBool(),
                LastLogin = reader.GetNullableDateTime("LastLogin"),
                IsLogedIn = reader["IsLogedIn"].AsBool(),
                Token = reader["Token"].AsString(),
                IsDeleted = reader["IsDeleted"].AsBool(),
                CreatedBy = reader["CreatedBy"].AsString(),
                CreatedDate = reader.GetNullableDateTime("CreatedDate"),
                UpdatedBy = reader["UpdatedBy"].AsString(),
                UpdatedDate = reader.GetNullableDateTime("UpdatedDate")
            };

        private List<IDataParameter> Take(User user)
        {
            if (string.IsNullOrEmpty(user.Id))
                user.Id = Guid.NewGuid().ToString();

            var parameters = new List<IDataParameter>
            {
                QueryHelper.CreateSqlParameter("@userId", new Guid(user.Id), NpgsqlDbType.Uuid),
                QueryHelper.CreateSqlParameter("@firstName", user.FirstName, NpgsqlDbType.Varchar),
                QueryHelper.CreateSqlParameter("@lastName", user.LastName, NpgsqlDbType.Varchar),
                QueryHelper.CreateSqlParameter("@loginId", user.LoginId, NpgsqlDbType.Varchar),
                QueryHelper.CreateSqlParameter("@password", user.Password, NpgsqlDbType.Varchar),
                QueryHelper.CreateSqlParameter("@emailId", user.EmailId, NpgsqlDbType.Varchar),
                QueryHelper.CreateSqlParameter("@phoneNumber", user.PhoneNumber, NpgsqlDbType.Varchar),
                QueryHelper.CreateSqlParameter("@roleId", user.RoleId == null? (object)DBNull.Value: new Guid(user.RoleId), NpgsqlDbType.Uuid),
                QueryHelper.CreateSqlParameter("@isActive", user.IsActive, NpgsqlDbType.Boolean),
                QueryHelper.CreateSqlParameter("@isSuperUser", user.IsSuperUser, NpgsqlDbType.Boolean),
                QueryHelper.CreateSqlParameter("@lastLogin", user.LastLogin, NpgsqlDbType.Timestamp),
                QueryHelper.CreateSqlParameter("@isLogedIn", user.IsLogedIn, NpgsqlDbType.Boolean),
                QueryHelper.CreateSqlParameter("@token", user.Token, NpgsqlDbType.Varchar),
                QueryHelper.CreateSqlParameter("@isDeleted", user.IsDeleted, NpgsqlDbType.Boolean),
                QueryHelper.CreateSqlParameter("@createdBy", user.CreatedBy, NpgsqlDbType.Varchar),
                QueryHelper.CreateSqlParameter("@createdDate", user.CreatedDate, NpgsqlDbType.Timestamp),
                QueryHelper.CreateSqlParameter("@updatedBy", user.UpdatedBy, NpgsqlDbType.Varchar),
                QueryHelper.CreateSqlParameter("@updatedDate", user.UpdatedDate, NpgsqlDbType.Timestamp)
            };

            return parameters;
        }

        public async Task<long> GetActiveUsersCount()
        {
            string sql = @"SELECT COUNT(*) FROM Users WHERE IsLogedIn = true";

            return Convert.ToInt64(await _queryHelper.ExecuteScalar(sql, null));
        }

        public async Task<IEnumerable<User>> GetOrphanAccounts(SearchFilter searchFilter = null, string orderBy = null, SortOrder sortOrder = SortOrder.Descending, int? page = null, int? pageSize = null, string[] fields = null)
        {
            var columns = "*";

            if (fields != null && fields.Length > 0)
                columns = string.Join(",", fields);

            string sql = $"SELECT {columns} FROM Users";

            if (searchFilter?.Filters?.Count() > 0)
            {
                sql += $" WHERE ({ExpressionBuilder.BuildWhereExpression(searchFilter)}) AND LoginId  NOT LIKE 'a%' AND LoginId NOT LIKE 's%' AND IsDeleted = false";
            }
            else
                sql += $" WHERE LoginId  NOT LIKE 'a%' AND LoginId NOT LIKE 's%' AND IsDeleted = false";

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

            var users = (await _queryHelper.Read(sql, null, Make)).ToList();

            foreach (var user in users)
            {
                user.Groups = (await GetGroups(user.Id)).ToList();
            }

            return users;
        }

        private readonly Func<IDataReader, string> MakeGroupIds = reader => reader["GroupId"].AsString();

        public async Task<long> GetOrphanAccountsCount(SearchFilter searchFilter = null)
        {
            var columns = "*";
            string sql = $"SELECT {columns} FROM [dbo].[Users]";

            if (searchFilter?.Filters?.Count() > 0)
            {
                sql += $" WHERE ({ExpressionBuilder.BuildWhereExpression(searchFilter)}) AND  [LoginId]  NOT LIKE 'a%' AND [LoginId] NOT LIKE 's%' AND [IsDeleted] = 0";
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

        public Task<IEnumerable<User>> GetAccountsMorethen30days()
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<User>> GetAccountsMorethen90days()
        {
            throw new NotImplementedException();
        }
    }
}
