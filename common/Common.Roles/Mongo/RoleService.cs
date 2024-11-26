using Common.Config;
using Common.DataAccess;
using Common.DataAccess.Mongo;
using Common.Roles.Models;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Common.Roles.Mongo
{
    public class RoleService : IRoleService
    {
        private readonly IRepository<Role> _rolesRepository;
        private const string RolesRepository = "Roles";

        private readonly IRepository<RoleMatrix> _roleMatrixRepository;
        private const string RoleMatrixRepository = "RoleMatrix";

        public RoleService(IOptions<DbConfig> dbConfig)
        {
            var databaseConfig = dbConfig.Value;

            var dbSettings = new DbSettings { ConnectionString = databaseConfig.ConnectionString, Database = databaseConfig.Database };

            _rolesRepository = new MongoRepository<Role>(dbSettings, RolesRepository);

            _roleMatrixRepository = new MongoRepository<RoleMatrix>(dbSettings, RoleMatrixRepository);
        }

        public RoleService(DbConfig dbConfig)
        {
            var dbSettings = new DbSettings { ConnectionString = dbConfig.ConnectionString, Database = dbConfig.Database };

            _rolesRepository = new MongoRepository<Role>(dbSettings, RolesRepository);
        }

        public async Task<Role> GetRole(string roleId)
        {
            if (string.IsNullOrEmpty(roleId)) return null;

            return await _rolesRepository.GetByIdAsync(roleId);
        }
        public async Task<long> GetRoleMatrixCount(SearchFilter searchFilter = null)
        {
            //string sql = $";WITH RoleMatrix(RoleName,RoleId,RoleDescription, ModuleId, ModuleName, PermissionName) As ( ";
            //sql += $" SELECT COUNT(*) FROM (SELECT r.Name as RoleName, r.Id as RoleId, r.Description as RoleDescription, m.Id as ModuleID, m.Name as ModuleName, p.Name as Permissionname FROM Roles r, Modules m, Role_Access ra, Permissions p Where r.Id = ra.RoleId AND m.Id = ra.ModuleId AND p.ModuleId = m.Id AND p.Id =ra.PermissionId AND r.IsDeleted = 0) as subSql ";
            ////sql += $" FROM Roles r";
            ////sql += $" FROM Roles r,  Modules m, Role_Access ra, Permissions p";
            ////sql += $" LEFT JOIN Role_Access ra on r.Id = ra.RoleId ";
            ////sql += $" LEFT JOIN Modules m on m.Id = ra.ModuleId";
            ////sql += $" LEFT JOIN Permissions p on p.ModuleId = m.Id AND p.Id =ra.PermissionId ";
            ////sql += $" Where r.IsDeleted = 0 ";
            ////sql += $" Where r.Id = ra.RoleId AND m.Id = ra.ModuleId AND p.ModuleId = m.Id AND p.Id =ra.PermissionId AND r.IsDeleted = 0 ";

            //if (searchFilter?.Filters?.Count > 0)
            //{
            //    sql += $" WHERE ({ExpressionBuilder.BuildWhereExpression(searchFilter)})";
            //}

            //sql += $" GROUP BY RoleName,RoleId, RoleDescription, ModuleID, ModuleName, PermissionName ) SELECT ee.RoleName,ee.RoleDescription, ee.ModuleName, STUFF(( SELECT ',' + e.PermissionName FROM RoleMatrix e where e.ModuleId =ee.ModuleId and e.RoleID =ee.RoleID FOR XML PATH('') ) ,1,1,'') AS AccessPermissions  from RoleMatrix ee GROUP BY ee.RoleName,ee.RoleDescription,ee.ModuleName,ee.ModuleId, ee.RoleId";
            //var count = Convert.ToInt64((await _queryHelper.ExecuteScalar(sql)));
            return 0;
        }
        public async Task<IEnumerable<Role>> GetRoles(SearchFilter searchFilter = null, string orderBy = null, SortOrder sortOrder = SortOrder.Descending, int? page = null, int? pageSize = null, string[] fields = null)
        {
            Expression<Func<Role, bool>> filterExpression = x => true;

            if (searchFilter != null && searchFilter.Filters.Count > 0)
                filterExpression = ExpressionBuilder.GetExpression<Role>(searchFilter);

            IList<Role> roles = null;

            if (page != null && pageSize != null)
                roles = (await _rolesRepository.GetPaginatedAsync(filterExpression, orderBy, sortOrder, (int)page, (int)pageSize)).ToList();
            else
                roles = (await _rolesRepository.GetAllAsync(filterExpression, null, orderBy, sortOrder)).ToList();

            return roles;
        }
        public async Task<bool> DeleteRoleFlag(string roleId)
        {
            // const string sql = "DELETE FROM [dbo].[Roles] WHERE [Id] = @roleId";
            //const string sql = @"UPDATE [dbo].[Roles] SET [IsDeleted] = 1 WHERE [Id] = @roleId";

            //var parameters = new List<IDataParameter>
            //{
            //    QueryHelper.CreateSqlParameter("@roleId", Guid.Parse(roleId), SqlDbType.UniqueIdentifier)
            //};

            //return await _queryHelper.ExecuteNonQuery(sql, parameters) == 1;
            return false;
        }
        public async Task<long> GetRolesCount(SearchFilter searchFilter = null)
        {
            Expression<Func<Role, bool>> filterExpression = x => true;

            if (searchFilter != null && searchFilter.Filters.Count > 0)
            {
                filterExpression = ExpressionBuilder.GetExpression<Role>(searchFilter);
            }

            return await _rolesRepository.CountAsync(filterExpression);
        }

        public async Task<string> CheckNameExists(string name)
        {
            var role = await _rolesRepository.GetOneAsync(x => x.Name == name);

            return role?.Id;
        }

        public async Task<Role> SaveRole(Role role)
        {
            return await _rolesRepository.UpdateOneAsync(role);
        }

        public async Task<bool> DeleteRole(string roleId)
        {
            return (await _rolesRepository.DeleteByIdAsync(roleId)) == 1;
        }

        public async Task<IEnumerable<RoleMatrix>> GetRoleMatrix(SearchFilter searchFilter = null, string orderBy = null, SortOrder sortOrder = SortOrder.Descending, int? page = null, int? pageSize = null, string[] fields = null)
        {
            Expression<Func<RoleMatrix, bool>> filterExpression = x => true;

            if (searchFilter != null && searchFilter.Filters.Count > 0)
                filterExpression = ExpressionBuilder.GetExpression<RoleMatrix>(searchFilter);

            IList<RoleMatrix> roles = null;

            if (page != null && pageSize != null)
                roles = (await _roleMatrixRepository.GetPaginatedAsync(filterExpression, orderBy, sortOrder, (int)page, (int)pageSize)).ToList();
            else
                roles = (await _roleMatrixRepository.GetAllAsync(filterExpression, null, orderBy, sortOrder)).ToList();

            return roles;
        }

    }
}
