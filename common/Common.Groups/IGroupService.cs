using Common.DataAccess;
using Common.Groups.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Common.Groups
{
    public interface IGroupService
    {
        Task<Group> GetGroup(string groupId);
        Task<IEnumerable<Group>> GetGroups(string loginUserId, bool isSuperAdmin, SearchFilter searchFilter = null, string orderBy = null, SortOrder sortOrder = SortOrder.Descending, int? page = null, int? pageSize = null, string[] fields = null);
        Task<IEnumerable<Group>> GetGroups(SearchFilter searchFilter = null, string orderBy = null, SortOrder sortOrder = SortOrder.Descending, int? page = null, int? pageSize = null, string[] fields = null);
        Task<long> GetGroupsCount(string loginUserId, bool isSuperAdmin, SearchFilter searchFilter = null);
        Task<string> CheckNameExists(string name);
        Task<Group> SaveGroup(Group group);
        Task<bool> DeleteGroup(string groupId);
        Task<bool> DeleteGroupFlag(string groupId);
    }
}
