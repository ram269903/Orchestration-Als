using Common.Config;
using Common.DataAccess;
using Common.DataAccess.Mongo;
using Common.Users.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Common.Users.Mongo
{
    public class UserService : IUserService
    {
        private readonly IRepository<User> _usersRepository;
        private readonly IRepository<LoginTracker> _loginTrackerRepository;
        private const string UsersRepository = "Users";
        private const string LoginTrackerRepository = "LoginTracker";

        public UserService(IOptions<DbConfig> dbConfig)
        {
            var databaseConfig = dbConfig.Value;

            var dbSettings = new DbSettings { ConnectionString = databaseConfig.ConnectionString, Database = databaseConfig.Database };

            _usersRepository = new MongoRepository<User>(dbSettings, UsersRepository);
            _loginTrackerRepository = new MongoRepository<LoginTracker>(dbSettings, LoginTrackerRepository);
        }

        public UserService(DbConfig dbConfig)
        {
            var databaseConfig = dbConfig;

            var dbSettings = new DbSettings { ConnectionString = databaseConfig.ConnectionString, Database = databaseConfig.Database };

            _usersRepository = new MongoRepository<User>(dbSettings, UsersRepository);
            _loginTrackerRepository = new MongoRepository<LoginTracker>(dbSettings, LoginTrackerRepository);
        }

        public void ResetLoginStatus()
        {
            var userCollection = _usersRepository.GetCollection();

            userCollection.UpdateManyAsync(
                  Builders<User>.Filter.Eq("IsLogedIn", true),
                  Builders<User>.Update.Set("IsLogedIn", false));

            //return updmanyresult.IsAcknowledged;
        }
        
        public async Task<User> GetUserByLoginId(string loginId)
        {
            if (string.IsNullOrEmpty(loginId)) return null;

            return await _usersRepository.GetOneAsync(x => x.LoginId.ToLower() == loginId.ToLower());
        }
        public async Task<bool> DeleteUserFlag(string userId)
        {
            // const string sql = "DELETE FROM [dbo].[Users] WHERE [Id] = @userId";
            //const string sql = @"UPDATE [dbo].[Users] SET [IsDeleted] = 1 WHERE [Id] = @userId";

            //var parameters = new List<IDataParameter>
            //{
            //    QueryHelper.CreateSqlParameter("@userId", Guid.Parse(userId), SqlDbType.UniqueIdentifier)
            //};

            //return await _queryHelper.ExecuteNonQuery(sql, parameters) == 1;

            return false;
        }
        public async Task<User> GetUserByToken(string token)
        {
            if (string.IsNullOrEmpty(token)) return null;

            return await _usersRepository.GetOneAsync(x => x.Token == token);
        }
        public async Task<bool> IsDelete(string userId)
        {
            //const string sql = @"SELECT * FROM Users WHERE Id = @userId AND IsDeleted = true";

            //var parameters = new List<IDataParameter>
            //{
            //    QueryHelper.CreateSqlParameter("@userId", Guid.Parse(userId), NpgsqlDbType.Uuid)
            //};

            //var count = Convert.ToInt64(await _queryHelper.ExecuteScalar(sql));

            //if (count > 0)
            //    return true;
            //else
            //    return false;

            return false;
        }

        public async Task<User> GetUser(string userId)
        {
            if (string.IsNullOrEmpty(userId)) return null;

            return await _usersRepository.GetByIdAsync(userId);
        }

        public async Task<long> GetUsersCount(SearchFilter searchFilter = null)
        {
            Expression<Func<User, bool>> filterExpression = x => true;

            if (searchFilter != null && searchFilter.Filters.Count > 0)
            {
                filterExpression = ExpressionBuilder.GetExpression<User>(searchFilter);

                //var filter = new Filter<User>();

                //foreach (var item in searchFilter.Filters)
                //{
                //    filter.By(item.PropertyName, Operation.Contains, item.Value, Connector.And);
                //}

                //filter.By("Name", Operation.Contains, " John");

                //filterExpression = new FilterBuilder().GetExpression<User>(filter);
            }

            return await _usersRepository.CountAsync(filterExpression);
        }

        public async Task<IEnumerable<User>> GetUsers(SearchFilter searchFilter = null, string orderBy = null, SortOrder sortOrder = SortOrder.Descending, int? page = null, int? pageSize = null, string[] fields = null)
        {
            Expression<Func<User, bool>> filterExpression = x => true;

            if (searchFilter != null && searchFilter.Filters.Count > 0)
                filterExpression = ExpressionBuilder.GetExpression<User>(searchFilter);

            IList<User> users = null;

            if (page != null && pageSize != null)
                users = (await _usersRepository.GetPaginatedAsync(filterExpression, orderBy, sortOrder, (int)page, (int)pageSize)).ToList();
            else
                users = (await _usersRepository.GetAllAsync(filterExpression, null, orderBy, sortOrder)).ToList();

            return users;
        }

        public async Task<string> CheckLoginIdExists(string loginId)
        {
            var user = await _usersRepository.GetOneAsync(x => x.LoginId == loginId);

            return user?.Id;
        }

        public async Task<User> SaveUser(User user)
        {
            return await _usersRepository.UpdateOneAsync(user);
        }

        public async void UpdateLoginUserCount() 
        {

            var loginCount = await _usersRepository.CountAsync(x => x.IsLogedIn == true);
            var loginTracker = await _loginTrackerRepository.GetOneAsync(x => x.Date == DateTime.Now.Date);

            if (loginTracker == null)
                await _loginTrackerRepository.UpdateOneAsync(new LoginTracker { Date = DateTime.Now.Date, MaxLoginUsers = loginCount });
            else
            {
                if (loginCount > loginTracker.MaxLoginUsers)
                {
                    loginTracker.MaxLoginUsers = loginCount;
                    await _loginTrackerRepository.UpdateOneAsync(loginTracker);
                }
            }
        }
       
        public async Task<bool> DeleteUserByLoginId(string loginId)
        {
            var user = await GetUserByLoginId(loginId);

            return (await _usersRepository.DeleteByIdAsync(user.Id)) == 1;
        }

        public async Task<bool> DeleteUser(string userId)
        {
            return (await _usersRepository.DeleteByIdAsync(userId)) == 1;
        }

        public void UpdateIsLogedIn(User user)
        {
            var userCollection = _usersRepository.GetCollection();

            userCollection.UpdateManyAsync(
                  Builders<User>.Filter.Eq("Id", user.Id),
                  Builders<User>.Update.Set("IsLogedIn", false));
        }

        public void UpdateLastLogin(User user)
        {
            var userCollection = _usersRepository.GetCollection();

            userCollection.UpdateManyAsync(
                  Builders<User>.Filter.Eq("Id", user.Id),
                  Builders<User>.Update.Set("IsLogedIn", user.IsLogedIn).Set("LastLogin", user.LastLogin));
        }

        public async Task<long> GetActiveUsersCount()
        {
            return await _usersRepository.CountAsync(x=>x.IsLogedIn == true);
        }

        public Task<long> GetUserGroupCount(SearchFilter searchFilter = null)
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<User>> GetOrphanAccounts(SearchFilter searchFilter = null, string orderBy = null, SortOrder sortOrder = SortOrder.Descending, int? page = null, int? pageSize = null, string[] fields = null)
        {
            Expression<Func<User, bool>> filterExpression = x => true;

            if (searchFilter != null && searchFilter.Filters.Count > 0)
                filterExpression = ExpressionBuilder.GetExpression<User>(searchFilter);

            IList<User> users = null;

            if (page != null && pageSize != null)
                users = (await _usersRepository.GetPaginatedAsync(filterExpression, orderBy, sortOrder, (int)page, (int)pageSize)).ToList();
            else
                users = (await _usersRepository.GetAllAsync(filterExpression, null, orderBy, sortOrder)).ToList();//Need to change here

            return users;
        }


        public async Task<long> GetOrphanAccountsCount(SearchFilter searchFilter = null)
        {
            Expression<Func<User, bool>> filterExpression = x => true;

            if (searchFilter != null && searchFilter.Filters.Count > 0)
            {
                filterExpression = ExpressionBuilder.GetExpression<User>(searchFilter);

            }

            return await _usersRepository.CountAsync(filterExpression);
        }
        public async Task<DateTime> GetLastActionTime(string loginId)
        {
            // string sql = $"select top 1 ActivityOn from ActivityLogs where loginId='{loginId.ToLower()}' order by ActivityOn desc";
            //string sql = $"select LastActionTime from Users where LoginId='{loginId.ToLower()}'";
            //return Convert.ToDateTime(await _queryHelper.ExecuteScalar(sql, null));
            return DateTime.Now;
        }

        public async Task<DateTime> UpdateLastActionTime(string loginId)
        {
            //string sql = $"update users set LastActionTime='{DateTime.Now.ToString("MM-dd-yyyy HH:mm:ss")}' where LoginId='{loginId.ToLower()}'";
            //return Convert.ToDateTime(await _queryHelper.ExecuteScalar(sql, null));
            return DateTime.Now;
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
