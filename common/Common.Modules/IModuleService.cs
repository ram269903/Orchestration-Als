using Common.DataAccess;
using Common.Modules.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Common.Modules
{
    public interface IModuleService
    {
        Task<Module> GetModule(string moduleId);
        Task<IEnumerable<Module>> GetModules(SearchFilter searchFilter = null, string orderBy = null, SortOrder sortOrder = SortOrder.Descending, int? page = null, int? pageSize = null, string[] fields = null);
        Task<long> GetModulesCount(SearchFilter searchFilter = null);
        Task<Module> SaveModule(Module module);
        Task<bool> DeleteModule(string moduleId);
        Task<Permission> GetPermissionsName(string v);

    }
}
