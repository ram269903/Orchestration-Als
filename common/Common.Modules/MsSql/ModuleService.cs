using Common.Config;
using Common.DataAccess;
using Common.DataAccess.MsSql;
using Common.DataAccess.RDBMS;
using Common.Modules.Models;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace Common.Modules.MsSql
{
    public class ModuleService : IModuleService
    {
        private readonly QueryHelper _queryHelper = null;

        public ModuleService(IOptions<DbConfig> dbConfig)
        {
            var databaseConfig = dbConfig.Value;

            _queryHelper = new QueryHelper(databaseConfig.ConnectionString);
        }

        public ModuleService(DbConfig dbConfig)
        {
            _queryHelper = new QueryHelper(dbConfig.ConnectionString);
        }
        //for AuditTrail

        public async Task<Permission> GetPermissionsName(string permissionId)
        {
            try
            {
                const string permissionsSql = @"SELECT * FROM [dbo].[Permissions] WHERE [Id] = @permissionId order by name";

                var parameters = new List<IDataParameter>
            {
                QueryHelper.CreateSqlParameter("@permissionId", permissionId, SqlDbType.NVarChar)
            };

                var data= (await _queryHelper.Read(permissionsSql, parameters, MakePermission)).FirstOrDefault();

                return data;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        //END
        public async Task<Module> GetModule(string moduleId)
        {
            if (string.IsNullOrEmpty(moduleId)) return null;

            const string moduleSql = @"SELECT * FROM [dbo].[Modules] WHERE [Id] = @moduleId AND [IsDeleted] = 0";

            var parameters = new List<IDataParameter>
            {
                QueryHelper.CreateSqlParameter("@moduleId", Guid.Parse(moduleId), SqlDbType.UniqueIdentifier)
            };

            var module = (await _queryHelper.Read(moduleSql, parameters, Make))?.FirstOrDefault();

            if (module == null) return null;

            module.Features = (await GetFeatures(moduleId)).ToList();
            module.Permissions = (await GetPermissions(moduleId)).ToList();

            return module;
        }

        public async Task<IEnumerable<Module>> GetModules(SearchFilter searchFilter = null, string orderBy = null, SortOrder sortOrder = SortOrder.Descending, int? page = null, int? pageSize = null, string[] fields = null)
        {
            var columns = "*";

            if (fields != null && fields.Length > 0)
                columns = string.Join(",", fields);

            string sql = $"SELECT {columns} FROM [dbo].[Modules]";

            if (searchFilter?.Filters?.Count() > 0)
            {
                sql += $" WHERE ({ExpressionBuilder.BuildWhereExpression(searchFilter)}) AND [IsDeleted] = 0";
            }
            else
                sql += $" WHERE [IsDeleted] = 0";

            if (!string.IsNullOrEmpty(orderBy))
            {
                sql += $" ORDER BY [{orderBy}]";
            }

            if (page != null && pageSize != null)
            {
                if (string.IsNullOrEmpty(orderBy))
                    sql += $" ORDER BY [Id]";

                var offset = pageSize * (page - 1);

                sql += $" OFFSET {offset} ROWS FETCH NEXT {pageSize} ROWS ONLY";
            }

            var modules = (await _queryHelper.Read(sql, null, Make))?.ToList();

            if (modules == null) return null;

            foreach (var module in modules)
            {
                module.Features = (await GetFeatures(module.Id)).ToList();
                module.Permissions = (await GetPermissions(module.Id)).ToList();
            }

            return modules;
        }

        private async Task<IEnumerable<Feature>> GetFeatures(string moduleId) 
        {
            const string featuresSql = @"SELECT * FROM [dbo].[Features] WHERE [ModuleId] = @moduleId order by name ";

            var parameters = new List<IDataParameter>
            {
                QueryHelper.CreateSqlParameter("@moduleId", moduleId, SqlDbType.NVarChar)
            };

            return await _queryHelper.Read(featuresSql, parameters, MakeFeature);

        }

        private async Task<IEnumerable<Permission>> GetPermissions(string moduleId)
        {
            const string permissionsSql = @"SELECT * FROM [dbo].[Permissions] WHERE [ModuleId] = @moduleId order by name";

            var parameters = new List<IDataParameter>
            {
                QueryHelper.CreateSqlParameter("@moduleId", moduleId, SqlDbType.NVarChar)
            };

            return await _queryHelper.Read(permissionsSql, parameters, MakePermission);

        }

        public async Task<long> GetModulesCount(SearchFilter searchFilter = null)
        {
            string sql = @"SELECT COUNT(*) FROM [dbo].[Modules]";

            if (searchFilter?.Filters?.Count() > 0)
            {
                sql += $" WHERE ({ExpressionBuilder.BuildWhereExpression(searchFilter)}) AND [IsDeleted] = 0";
            }
            else
                sql += $" WHERE [IsDeleted] = 0";

            var result = await _queryHelper.ExecuteScalar(sql, null);
            return Convert.ToInt64(result);
        }

        public async Task<Module> SaveModule(Module module)
        {
            if (string.IsNullOrEmpty(module.Id))
                return await InsertModule(module);
            else
                return await UpdateModule(module);
        }

        public async Task<bool> DeleteModule(string moduleId)
        {
            const string sql = "DELETE FROM [dbo].[Modules] WHERE [Id] = @moduleId";
            //const string sql = @"UPDATE [dbo].[Modules] SET [IsDeleted] = 1 WHERE [Id] = @moduleId";

            var parameters = new List<IDataParameter>
            {
                QueryHelper.CreateSqlParameter("@moduleId", Guid.Parse(moduleId), SqlDbType.UniqueIdentifier)
            };

            return await _queryHelper.ExecuteNonQuery(sql, parameters) == 1;
        }

        private async Task<Module> InsertModule(Module module)
        {
            const string sql = @"INSERT [dbo].[Modules] (
                                [Name],
                                [Description],
                                [ParentId],
                                [CreatedBy],
                                [CreatedDate],
                                [UpdatedBy],
                                [UpdatedDate],
                                [IsDeleted]) 
                            VALUES (
                                @name,
                                @description,
                                @parentId, 
                                @createdBy,
                                @createdDate,
                                @updatedBy,
                                @updatedDate,
                                @isDeleted)";

            await _queryHelper.ExecuteScalar(sql, Take(module));

            foreach (var feature in module.Features)
            {
                _ = await InsertFeature(module.Id, feature);
            }

            foreach (var permission in module.Permissions)
            {
                _ = await InsertPermission(module.Id, permission);
            }

            return module;
        }

        private async Task<Module> UpdateModule(Module module)
        {
            const string sql = @"UPDATE [dbo].[Modules]
                                SET 
                                    [Name] = @name,
                                    [Description] = @description,
                                    [ParentId] = @parentId,
                                    [CreatedBy] = @createdBy,
                                    [CreatedDate] = @createdDate,
                                    [UpdatedBy] = @updatedBy,
                                    [UpdatedDate] = @updatedDate,
                                    [IsDeleted] = @isDeleted
                               WHERE 
                                    [Id] = @id";

            _ = await _queryHelper.ExecuteNonQuery(sql, Take(module));

            //foreach (var feature in module.Features)
            //{
            //    _ = await UpdateFeature(module.Id, feature);
            //}

            //foreach (var permission in module.Permissions)
            //{
            //    _ = await UpdatePermission(module.Id, permission);
            //}

            return module;
        }

        private async Task<Feature> InsertFeature(string moduleId, Feature feature)
        {
            var sql = $@"INSERT [dbo].[Features] (
                                [ModuleId],
                                [Code],
                                [Name],
                                [Description]) 
                            VALUES (
                                {moduleId},
                                @code,
                                @name,
                                @description)";

            await _queryHelper.ExecuteScalar(sql, TakeFeature(feature));

            return feature;
        }

        private async Task<Permission> InsertPermission(string moduleId, Permission permission)
        {
            var sql = $@"INSERT [dbo].[Permissions] (
                                [ModuleId],
                                [Code],
                                [Name],
                                [Description]) 
                            VALUES (
                                {moduleId},
                                @code,
                                @name,
                                @description)";

            await _queryHelper.ExecuteScalar(sql, TakePermission(permission));

            return permission;
        }

        private readonly Func<IDataReader, Module> Make = reader =>
            new Module
            {
                Id = reader["Id"].AsString(),
                Name = reader["Name"].AsString(),
                Description = reader["Description"].AsString(),
                ParentId = reader["ParentId"].AsString(),
                IsDeleted = reader["IsDeleted"].AsBool(),
                CreatedBy = reader["CreatedBy"].AsString(),
                CreatedDate = reader.GetNullableDateTime("CreatedDate"),
                UpdatedBy = reader["UpdatedBy"].AsString(),
                UpdatedDate = reader.GetNullableDateTime("UpdatedDate")
            };

        private readonly Func<IDataReader, Feature> MakeFeature = reader =>
            new Feature
            {
                Id = reader["Id"].AsString(),
                Code = reader["Code"].AsString(),
                Name = reader["Name"].AsString(),
                Description = reader["Description"].AsString(),
            };

        private readonly Func<IDataReader, Permission> MakePermission = reader =>
            new Permission
            {
                Id = reader["Id"].AsString(),
                Code = reader["Code"].AsString(),
                Name = reader["Name"].AsString(),
                Description = reader["Description"].AsString(),
            };

        private List<IDataParameter> Take(Module module)
        {
            if (string.IsNullOrEmpty(module.Id))
                module.Id = Guid.NewGuid().ToString();

            var parameters = new List<IDataParameter>
            {
                QueryHelper.CreateSqlParameter("@id", new Guid(module.Id), SqlDbType.UniqueIdentifier),
                QueryHelper.CreateSqlParameter("@name", module.Name, SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@description", module.Description, SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@parentId", module.ParentId, SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@isDeleted", module.IsDeleted == true? 1: 0, SqlDbType.Bit),
                QueryHelper.CreateSqlParameter("@createdBy", module.CreatedBy, SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@createdDate", module.CreatedDate, SqlDbType.DateTime2),
                QueryHelper.CreateSqlParameter("@updatedBy", module.UpdatedBy, SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@updatedDate", module.UpdatedDate, SqlDbType.DateTime2)
            };

            return parameters;
        }

        private List<IDataParameter> TakeFeature(Feature feature)
        {
            if (string.IsNullOrEmpty(feature.Id))
                feature.Id = Guid.NewGuid().ToString();

            var parameters = new List<IDataParameter>
            {
                QueryHelper.CreateSqlParameter("@id", new Guid(feature.Id), SqlDbType.UniqueIdentifier),
                QueryHelper.CreateSqlParameter("@code", feature.Code, SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@name", feature.Name, SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@description", feature.Description, SqlDbType.NVarChar)
            };

            return parameters;
        }

        private List<IDataParameter> TakePermission(Permission permission)
        {
            if (string.IsNullOrEmpty(permission.Id))
                permission.Id = Guid.NewGuid().ToString();

            var parameters = new List<IDataParameter>
            {
                QueryHelper.CreateSqlParameter("@id", new Guid(permission.Id), SqlDbType.UniqueIdentifier),
                QueryHelper.CreateSqlParameter("@code", permission.Code, SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@name", permission.Name, SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@description", permission.Description, SqlDbType.NVarChar)
            };

            return parameters;
        }
    }
}
