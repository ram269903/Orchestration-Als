using Common.DataAccess;
using Common.Roles.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Common.Roles
{
    public interface IRoleService
    {
        Task<Role> GetRole(string roleId);
        Task<IEnumerable<Role>> GetRoles(SearchFilter searchFilter = null, string orderBy = null, SortOrder sortOrder = SortOrder.Descending, int? page = null, int? pageSize = null, string[] fields = null);
        Task<long> GetRolesCount(SearchFilter searchFilter = null);
        Task<string> CheckNameExists(string name);
        Task<Role> SaveRole(Role role);
        Task<bool> DeleteRole(string roleId);
        Task<bool> DeleteRoleFlag(string roleId);
        Task<long> GetRoleMatrixCount(SearchFilter searchFilter=null);
        Task<IEnumerable<RoleMatrix>> GetRoleMatrix(SearchFilter searchFilter = null, string orderBy = null, SortOrder sortOrder = SortOrder.Descending, int? page = null, int? pageSize = null, string[] fields = null);
    }
}
