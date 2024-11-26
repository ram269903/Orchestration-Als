using Common.DataAccess;
using Common.Users.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Common.Users
{
    public interface IUserService
    {
        void ResetLoginStatus();
        Task<User> GetUserByLoginId(string loginId);
        Task<User> GetUserByToken(string token);
        Task<User> GetUser(string userId);
        Task<bool> IsDelete(string userId);
        Task<long> GetUsersCount(SearchFilter searchFilter = null);
        Task<long> GetActiveUsersCount();
        Task<long> GetUserGroupCount(SearchFilter searchFilter = null);
        Task<IEnumerable<User>> GetUsers(SearchFilter searchFilter = null, string orderBy = null, SortOrder sortOrder = SortOrder.Descending, int? page = null, int? pageSize = null, string[] fields = null);
        Task<string> CheckLoginIdExists(string loginId);
        Task<User> SaveUser(User user);
        void UpdateIsLogedIn(User user);
        void UpdateLastLogin(User user);
        void UpdateLoginUserCount();
        Task<bool> DeleteUserByLoginId(string loginId);
        Task<bool> DeleteUser(string userId);
        Task<bool> DeleteUserFlag(string userId);
        Task<IEnumerable<User>> GetOrphanAccounts(SearchFilter searchFilter = null, string orderBy = null, SortOrder sortOrder = SortOrder.Descending, int? page = null, int? pageSize = null, string[] fields = null);
        Task<long> GetOrphanAccountsCount(SearchFilter searchFilter = null);
        Task<DateTime> GetLastActionTime(string loginId);
        Task<DateTime> UpdateLastActionTime(string loginId);

        Task<IEnumerable<User>> GetAccountsMorethen30days();
        Task<IEnumerable<User>> GetAccountsMorethen90days();
    }
}
