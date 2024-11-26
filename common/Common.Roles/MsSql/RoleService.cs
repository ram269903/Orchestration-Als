using Common.Config;
using Common.DataAccess;
using Common.DataAccess.MsSql;
using Common.DataAccess.RDBMS;
using Common.Roles.Models;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace Common.Roles.MsSql
{
    public class RoleService : IRoleService
    {
        private readonly QueryHelper _queryHelper = null;
        private readonly Func<IDataReader, RoleMatrix> MakeRoleMatrix = reader =>
          new RoleMatrix
          {
              RoleName = reader["RoleName"].ToString(),
              RoleDescription = reader["RoleDescription"].ToString(),
              ModuleName = reader["ModuleName"].ToString(),
              AccessPermissions = reader["AccessPermissions"].ToString()

          };

        public RoleService(IOptions<DbConfig> dbConfig)
        {
            var databaseConfig = dbConfig.Value;

            _queryHelper = new QueryHelper(databaseConfig.ConnectionString);
        }

        public RoleService(DbConfig dbConfig)
        {
            _queryHelper = new QueryHelper(dbConfig.ConnectionString);
        }

        public async Task<Role> GetRole(string roleId)
        {
            if (string.IsNullOrEmpty(roleId)) return null;

            const string sql = @"SELECT * FROM [dbo].[Roles] WHERE [Id] = @roleId AND [IsDeleted] = 0";

            var parameters = new List<IDataParameter>
            {
                QueryHelper.CreateSqlParameter("@roleId", Guid.Parse(roleId), SqlDbType.UniqueIdentifier)
            };

            var role = (await _queryHelper.Read(sql, parameters, Make)).FirstOrDefault();

            if (role == null) return null;

            role.ModulePermissions = (await GetModulePermissions(role.Id))?.ToList();

            return role;
        }

        public async Task<IEnumerable<Role>> GetRoles(SearchFilter searchFilter = null, string orderBy = null, SortOrder sortOrder = SortOrder.Descending, int? page = null, int? pageSize = null, string[] fields = null)
        {
            var columns = "*";

            if (fields != null && fields.Length > 0)
                columns = string.Join(",", fields);

            string sql = $"SELECT {columns} FROM [dbo].[Roles]";

            if (searchFilter?.Filters?.Count() > 0)
            {
                sql += $" WHERE ({ExpressionBuilder.BuildWhereExpression(searchFilter)}) AND [IsDeleted] = 0";
            }
            else
                sql += $" WHERE [IsDeleted] = 0";

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

            var roles = (await _queryHelper.Read(sql, null, Make))?.ToList();

            if (roles == null) return null;

            foreach (var role in roles)
            {
                role.ModulePermissions = (await GetModulePermissions(role.Id))?.ToList();
            }

            return roles;
        }

        private async Task<IEnumerable<ModulePermission>> GetModulePermissions(string roleId)
        {
            const string sql = @"SELECT * FROM [dbo].[Role_Access] WHERE [RoleId] = @roleId";

            var parameters = new List<IDataParameter>
            {
                QueryHelper.CreateSqlParameter("@roleId", roleId, SqlDbType.NVarChar)
            };

            var tempModulePermissions = await _queryHelper.Read(sql, parameters, MakeModulePermission);

            if (tempModulePermissions == null) return null;

            var modulePermissions = new List<ModulePermission>();

            if (tempModulePermissions != null && tempModulePermissions.Count() > 0)
            {
                var modulePermissionGroups = tempModulePermissions.ToList().GroupBy(x => x.ModuleId);

                foreach (var group in modulePermissionGroups)
                {
                    var moduleId = group.Key;
                    var permissionIds = new List<string>();
                    var permissionNames = new List<string>();
                    foreach (var groupedItem in group)
                    {
                        permissionIds.Add(groupedItem.PermissionId);
                    }

                    modulePermissions.Add(new ModulePermission { ModuleId = moduleId, PermissionIds = permissionIds });
                }
            }

            return modulePermissions;
        }

        public async Task<long> GetRolesCount(SearchFilter searchFilter = null)
        {
            string sql = @"SELECT COUNT(*) FROM [dbo].[Roles]";

            if (searchFilter?.Filters?.Count() > 0)
            {
                sql += $" WHERE ({ExpressionBuilder.BuildWhereExpression(searchFilter)}) AND [IsDeleted] = 0";
            }
            else
                sql += $" WHERE [IsDeleted] = 0";

            return Convert.ToInt64(await _queryHelper.ExecuteScalar(sql, null));
        }

        public async Task<string> CheckNameExists(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;

            const string sql = @"SELECT * FROM [dbo].[Roles] WHERE [Name] = @name AND [IsDeleted] = 0";

            var parameters = new List<IDataParameter>
            {
                QueryHelper.CreateSqlParameter("@name", name, SqlDbType.NVarChar)
            };

            var role = (await _queryHelper.Read(sql, parameters, Make)).FirstOrDefault();

            return role?.Id;
        }

        public async Task<Role> SaveRole(Role role)
        {
            if (string.IsNullOrEmpty(role.Id))
                return await InsertRole(role);
            else
                return await UpdateRole(role);
        }
       public async Task<bool> DeleteRoleFlag(string roleId)
        {
           // const string sql = "DELETE FROM [dbo].[Roles] WHERE [Id] = @roleId";
           const string sql = @"UPDATE [dbo].[Roles] SET [IsDeleted] = 1 WHERE [Id] = @roleId";

            var parameters = new List<IDataParameter>
            {
                QueryHelper.CreateSqlParameter("@roleId", Guid.Parse(roleId), SqlDbType.UniqueIdentifier)
            };

            return await _queryHelper.ExecuteNonQuery(sql, parameters) == 1;
        }
        public async Task<bool> DeleteRole(string roleId)
        {
            const string sql = "DELETE FROM [dbo].[Roles] WHERE [Id] = @roleId";
            //const string sql = @"UPDATE [dbo].[Roles] SET [IsDeleted] = 1 WHERE [Id] = @roleId";

            var parameters = new List<IDataParameter>
            {
                QueryHelper.CreateSqlParameter("@roleId", Guid.Parse(roleId), SqlDbType.UniqueIdentifier)
            };

            return await _queryHelper.ExecuteNonQuery(sql, parameters) == 1;

        }

        private async Task<Role> InsertRole(Role role)
        {
            const string sql = @"INSERT [dbo].[Roles] (
                                [Name],
                                [Description],
                                [GroupId],
                                [CreatedBy],
                                [CreatedDate],
                                [UpdatedBy],
                                [UpdatedDate],
                                [IsDeleted])
                            OUTPUT Inserted.ID
                            VALUES (
                                @name,
                                @description,
                                @groupId,
                                @createdBy,
                                @createdDate,
                                @updatedBy,
                                @updatedDate,
                                @isDeleted)";

            var id = await _queryHelper.ExecuteScalar(sql, Take(role));

            role.Id = id.ToString();

            if (role.ModulePermissions != null && role.ModulePermissions.Count() > 0)
                UpdateRoleAccess(role);

            return role;
        }

        private async Task<Role> UpdateRole(Role role)
        {
            const string sql = @"UPDATE [dbo].[Roles]
                                SET 
                                    [Name] = @name,
                                    [Description] = @description,
                                    [GroupId]= @groupId,
                                    [CreatedBy] = @createdBy,
                                    [CreatedDate] = @createdDate,
                                    [UpdatedBy] = @updatedBy,
                                    [UpdatedDate] = @updatedDate,
                                    [IsDeleted] = @isDeleted
                               WHERE 
                                    [Id] = @roleId";

            _ = await _queryHelper.ExecuteNonQuery(sql, Take(role));

            if (role.ModulePermissions != null)
                UpdateRoleAccess(role);

            return role;
        }

        private async void UpdateRoleAccess(Role role)
        {
            const string deleteSql = @"DELETE FROM [dbo].[Role_Access] WHERE [RoleId] = @roleId";

            var parameters = new List<IDataParameter>
            {
                QueryHelper.CreateSqlParameter("@roleId", role.Id, SqlDbType.NVarChar)
            };

            _ = await _queryHelper.ExecuteNonQuery(deleteSql, parameters) == 1;

            var values = "";

            foreach (var modPermission in role.ModulePermissions)
            {
                var moduleId = modPermission.ModuleId;

                foreach (var permissionId in modPermission.PermissionIds)
                {
                    values += $"('{role.Id}', '{moduleId}', '{permissionId}'),";
                }

            }

            if (!string.IsNullOrEmpty(values))
            {
                values = values.Remove(values.Length - 1);

                var sql = $@"INSERT [dbo].[Role_Access] ([RoleId], [ModuleId], [PermissionId]) VALUES {values}";

                _ = await _queryHelper.ExecuteQuery(sql, null);
            }

        }

        private readonly Func<IDataReader, Role> Make = reader =>
            new Role
            {
                Id = reader["Id"].AsString(),
                Name = reader["Name"].AsString(),
                Description = reader["Description"].AsString(),
                GroupId = reader["GroupId"].AsString(),
                IsSuperRole = reader["IsSuperRole"].AsBool(),
                IsDeleted = reader["IsDeleted"].AsBool(),
                CreatedBy = reader["CreatedBy"].AsString(),
                CreatedDate = reader.GetNullableDateTime("CreatedDate"),
                UpdatedBy = reader["UpdatedBy"].AsString(),
                UpdatedDate = reader.GetNullableDateTime("UpdatedDate")
            };

        private readonly Func<IDataReader, ModPermission> MakeModulePermission = reader =>
            new ModPermission
            {
                ModuleId = reader["ModuleId"].AsString(),
                PermissionId = reader["PermissionId"].AsString()
            };

        private List<IDataParameter> Take(Role role)
        {
            if (string.IsNullOrEmpty(role.Id))
                role.Id = Guid.NewGuid().ToString();

            var parameters = new List<IDataParameter>
            {
                QueryHelper.CreateSqlParameter("@roleId", new Guid(role.Id), SqlDbType.UniqueIdentifier),
                QueryHelper.CreateSqlParameter("@name", role.Name, SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@description", role.Description, SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@isSuperRole", role.IsSuperRole == true? 1: 0, SqlDbType.Bit),
                QueryHelper.CreateSqlParameter("@groupId", role.GroupId, SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@isDeleted", role.IsDeleted == true? 1: 0, SqlDbType.Bit),
                QueryHelper.CreateSqlParameter("@createdBy", role.CreatedBy, SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@createdDate", role.CreatedDate, SqlDbType.DateTime2),
                QueryHelper.CreateSqlParameter("@updatedBy", role.UpdatedBy, SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@updatedDate", role.UpdatedDate, SqlDbType.DateTime2)
            };

            return parameters;
        }

        //private List<IDataParameter> TakePermission(ModPermission permission)
        //{
        //    if (string.IsNullOrEmpty(permission.Id))
        //        permission.Id = Guid.NewGuid().ToString();

        //    var parameters = new List<IDataParameter>
        //    {
        //        QueryHelper.CreateSqlParameter("@id", new Guid(permission.Id), SqlDbType.UniqueIdentifier),
        //        QueryHelper.CreateSqlParameter("@code", permission.Code, SqlDbType.NVarChar),
        //        QueryHelper.CreateSqlParameter("@name", permission.Name, SqlDbType.NVarChar),
        //        QueryHelper.CreateSqlParameter("@description", permission.Description, SqlDbType.NVarChar)
        //    };

        //    return parameters;
        //}
        public async Task<long> GetRoleMatrixCount(SearchFilter searchFilter = null)
        {
            string sql = $";WITH RoleMatrix(RoleName,RoleId,RoleDescription, ModuleId, ModuleName, PermissionName) As ( ";
            sql += $" SELECT * FROM (SELECT r.Name as RoleName, r.Id as RoleId, r.Description as RoleDescription, m.Id as ModuleID, m.Name as ModuleName, p.Name as Permissionname FROM Roles r, Modules m, Role_Access ra, Permissions p Where r.Id = ra.RoleId AND m.Id = ra.ModuleId AND p.ModuleId = m.Id AND p.Id =ra.PermissionId AND r.IsDeleted = 0) as subSql ";
            //sql += $" FROM Roles r";
            //sql += $" FROM Roles r,  Modules m, Role_Access ra, Permissions p";
            //sql += $" LEFT JOIN Role_Access ra on r.Id = ra.RoleId ";
            //sql += $" LEFT JOIN Modules m on m.Id = ra.ModuleId";
            //sql += $" LEFT JOIN Permissions p on p.ModuleId = m.Id AND p.Id =ra.PermissionId ";
            //sql += $" Where r.IsDeleted = 0 ";
            //sql += $" Where r.Id = ra.RoleId AND m.Id = ra.ModuleId AND p.ModuleId = m.Id AND p.Id =ra.PermissionId AND r.IsDeleted = 0 ";

            if (searchFilter?.Filters?.Count > 0)
            {
                sql += $" WHERE ({ExpressionBuilder.BuildWhereExpression(searchFilter)})";
            }

            sql += $" GROUP BY RoleName,RoleId, RoleDescription, ModuleID, ModuleName, PermissionName ) SELECT COUNT(*) FROM( SELECT ee.RoleName,ee.RoleDescription, ee.ModuleName, STUFF(( SELECT ',' + e.PermissionName FROM RoleMatrix e where e.ModuleId =ee.ModuleId and e.RoleID =ee.RoleID FOR XML PATH('') ) ,1,1,'') AS AccessPermissions  from RoleMatrix ee GROUP BY ee.RoleName,ee.RoleDescription,ee.ModuleName,ee.ModuleId, ee.RoleId) AS t";
            var count = Convert.ToInt64((await _queryHelper.ExecuteScalar(sql)));
            return count;
        }
        public async Task<IEnumerable<RoleMatrix>> GetRoleMatrix(SearchFilter searchFilter = null, string orderBy = null, SortOrder sortOrder = SortOrder.Descending, int? page = null, int? pageSize = null, string[] fields = null)
        {
            string sql = $" ; WITH RoleMatrix(RoleName,RoleId,RoleDescription, ModuleId, ModuleName, PermissionName) As ( ";
            sql += $" SELECT * FROM (SELECT r.Name as RoleName, r.Id as RoleId, r.Description as RoleDescription, m.Id as ModuleID, m.Name as ModuleName, p.Name as Permissionname FROM Roles r, Modules m, Role_Access ra, Permissions p Where r.Id = ra.RoleId AND m.Id = ra.ModuleId AND p.ModuleId = m.Id AND p.Id =ra.PermissionId AND r.IsDeleted = 0) as subSql ";
            //sql += $" FROM Roles r";
            //sql += $" FROM Roles r,  Modules m, Role_Access ra, Permissions p";
            //sql += $" LEFT JOIN Role_Access ra on r.Id = ra.RoleId ";
            //sql += $" LEFT JOIN Modules m on m.Id = ra.ModuleId";
            //sql += $" LEFT JOIN Permissions p on p.ModuleId = m.Id AND p.Id =ra.PermissionId ";
            //sql += $" Where r.IsDeleted = 0 ";
            //sql += $" Where r.Id = ra.RoleId AND m.Id = ra.ModuleId AND p.ModuleId = m.Id AND p.Id =ra.PermissionId AND r.IsDeleted = 0 ";

            if (searchFilter?.Filters?.Count > 0)
            {
                sql += $" WHERE ({ExpressionBuilder.BuildWhereExpression(searchFilter)})";
            }

            sql += $" GROUP BY RoleName,RoleId, RoleDescription, ModuleID, ModuleName, PermissionName ) SELECT ee.RoleName,ee.RoleDescription, ee.ModuleName, STUFF(( SELECT ',' + e.PermissionName FROM RoleMatrix e where e.ModuleId =ee.ModuleId and e.RoleID =ee.RoleID FOR XML PATH('') ) ,1,1,'') AS AccessPermissions  from RoleMatrix ee GROUP BY ee.RoleName,ee.RoleDescription,ee.ModuleName,ee.ModuleId, ee.RoleId";
            //sql += $" )";
            //sql += $" SELECT ee.RoleName,ee.RoleDescription, ee.ModuleName, STUFF((";
            //sql += $" SELECT ',' + e.PermissionName";
            //sql += $" FROM RoleMatrix e where e.ModuleId =ee.ModuleId and e.RoleID =ee.RoleID";
            //sql += $" FOR XML PATH('')";
            //sql += $" )";
            //sql += $" ,1,1,'') AS AccessPermissions  from RoleMatrix ee";


            //sql += $" GROUP BY ee.RoleName,ee.RoleDescription,ee.ModuleName,ee.ModuleId, ee.RoleId";


            if (!string.IsNullOrEmpty(orderBy))
            {
                if (!orderBy.ToLower().Contains("accesspermissions"))
                {
                    if (!orderBy.Contains("ee"))
                    {
                        var sort = sortOrder == SortOrder.Ascending ? "asc" : "desc";

                        sql += $" ORDER BY ee.{orderBy} {sort}";
                    }
                    else
                    {
                        var sort = sortOrder == SortOrder.Ascending ? "asc" : "desc";

                        sql += $" ORDER BY {orderBy} {sort}";
                    }
                }
                else
                {
                    var sort = sortOrder == SortOrder.Ascending ? "asc" : "desc";

                    sql += $" ORDER BY {orderBy} {sort}";
                }

            }

            if (page != null && pageSize != null)
            {
                if (string.IsNullOrEmpty(orderBy))
                    sql += $" ORDER BY ee.RoleId";

                var offset = pageSize * (page - 1);

                sql += $" OFFSET {offset} ROWS FETCH NEXT {pageSize} ROWS ONLY";
            }

            var roleMatrix = (await _queryHelper.Read(sql, null, MakeRoleMatrix))?.ToList();
            //var filter = ExpressionBuilder.GetExpression<RoleMatrix>(searchFilter);
            return roleMatrix;

        }
    }
}
