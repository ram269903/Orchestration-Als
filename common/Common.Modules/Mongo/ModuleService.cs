using Common.Config;
using Common.DataAccess;
using Common.DataAccess.Mongo;
using Common.Modules.Models;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Common.Modules.Mongo
{
    public class ModuleService : IModuleService
    {
        private readonly IRepository<Module> _modulesRepository;
        private const string ModulesRepository = "Modules";

        public ModuleService(IOptions<DbConfig> dbConfig)
        {
            var databaseConfig = dbConfig.Value;

            var dbSettings = new DbSettings { ConnectionString = databaseConfig.ConnectionString, Database = databaseConfig.Database };

            _modulesRepository = new MongoRepository<Module>(dbSettings, ModulesRepository);
        }

        public ModuleService(DbConfig dbConfig)
        {
            var dbSettings = new DbSettings { ConnectionString = dbConfig.ConnectionString, Database = dbConfig.Database };

            _modulesRepository = new MongoRepository<Module>(dbSettings, ModulesRepository);
        }

        public async Task<Module> GetModule(string moduleId)
        {
            if (string.IsNullOrEmpty(moduleId)) return null;

            return await _modulesRepository.GetByIdAsync(moduleId);
        }

        public async Task<IEnumerable<Module>> GetModules(SearchFilter searchFilter = null, string orderBy = null, SortOrder sortOrder = SortOrder.Descending, int? page = null, int? pageSize = null, string[] fields = null)
        {
            Expression<Func<Module, bool>> filterExpression = x => true;

            if (searchFilter != null && searchFilter.Filters.Count > 0)
                filterExpression = ExpressionBuilder.GetExpression<Module>(searchFilter);

            IList<Module> modules = null;

            if (page != null && pageSize != null)
                modules = (await _modulesRepository.GetPaginatedAsync(filterExpression, orderBy, sortOrder, (int)page, (int)pageSize)).ToList();
            else
                modules = (await _modulesRepository.GetAllAsync(filterExpression, null, orderBy, sortOrder)).ToList();

            return modules;
        }

        public async Task<long> GetModulesCount(SearchFilter searchFilter = null)
        {
            Expression<Func<Module, bool>> filterExpression = x => true;

            if (searchFilter != null && searchFilter.Filters.Count > 0)
            {
                filterExpression = ExpressionBuilder.GetExpression<Module>(searchFilter);
            }

            return await _modulesRepository.CountAsync(filterExpression);
        }

        public async Task<Module> SaveModule(Module module)
        {
            return await _modulesRepository.UpdateOneAsync(module);
        }

        public async Task<bool> DeleteModule(string moduleId)
        {
            return (await _modulesRepository.DeleteByIdAsync(moduleId)) == 1;
        }

        public Task<Permission> GetPermissionsName(string v)
        {
            throw new NotImplementedException();
        }
    }
}
