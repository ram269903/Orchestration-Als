using Common.Config;
using Common.DataAccess;
using Common.DataAccess.PostgreSql;
using Common.DataAccess.RDBMS;
using Common.Modules.Models;
using Microsoft.Extensions.Options;
using NpgsqlTypes;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace Common.Modules.PgSql
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

        public async Task<Module> GetModule(string moduleId)
        {
            if (string.IsNullOrEmpty(moduleId)) return null;

            const string moduleSql = @"SELECT * FROM Modules WHERE Id = @moduleId AND IsDeleted = false";

            var parameters = new List<IDataParameter>
            {
                QueryHelper.CreateSqlParameter("@moduleId", Guid.Parse(moduleId), NpgsqlDbType.Uuid)
            };

            var module = (await _queryHelper.Read(moduleSql, parameters, Make)).FirstOrDefault();

            module.Features = (await GetFeatures(moduleId)).ToList();
            module.Permissions = (await GetPermissions(moduleId)).ToList();

            return module;
        }

        public async Task<IEnumerable<Module>> GetModules(SearchFilter searchFilter = null, string orderBy = null, SortOrder sortOrder = SortOrder.Descending, int? page = null, int? pageSize = null, string[] fields = null)
        {
            var columns = "*";

            if (fields != null && fields.Length > 0)
                columns = string.Join(",", fields);

            string sql = $"SELECT {columns} FROM Modules";

            if (searchFilter?.Filters?.Count() > 0)
            {
                sql += $" WHERE ({ExpressionBuilder.BuildWhereExpression(searchFilter)}) AND IsDeleted = false";
            }
            else
                sql += $" WHERE IsDeleted = false";

            if (!string.IsNullOrEmpty(orderBy))
            {
                sql += $" ORDER BY {orderBy}";
            }

            if (page != null && pageSize != null)
            {
                if (string.IsNullOrEmpty(orderBy))
                    sql += $" ORDER BY Id";

                var offset = pageSize * (page - 1);

                sql += $" OFFSET {offset} ROWS FETCH NEXT {pageSize} ROWS ONLY";
            }

            var modules = (await _queryHelper.Read(sql, null, Make)).ToList();

            foreach (var module in modules)
            {
                module.Features = (await GetFeatures(module.Id)).ToList();
                module.Permissions = (await GetPermissions(module.Id)).ToList();
            }

            return modules;
        }

        private async Task<IEnumerable<Feature>> GetFeatures(string moduleId) 
        {
            const string featuresSql = @"SELECT * FROM Features WHERE ModuleId = @moduleId";

            var parameters = new List<IDataParameter>
            {
                QueryHelper.CreateSqlParameter("@moduleId", Guid.Parse(moduleId), NpgsqlDbType.Uuid)
            };

            return await _queryHelper.Read(featuresSql, parameters, MakeFeature);

        }

        private async Task<IEnumerable<Permission>> GetPermissions(string moduleId)
        {
            const string permissionsSql = @"SELECT * FROM Permissions";// WHERE ModuleId = @moduleId";

            var parameters = new List<IDataParameter>();
            //{
            //    QueryHelper.CreateSqlParameter("@moduleId", Guid.Parse(moduleId), NpgsqlDbType.Uuid)
            //};

            return await _queryHelper.Read(permissionsSql, parameters, MakePermission);

        }

        public async Task<long> GetModulesCount(SearchFilter searchFilter = null)
        {
            string sql = @"SELECT COUNT(*) FROM Modules";

            if (searchFilter?.Filters?.Count() > 0)
            {
                sql += $" WHERE ({ExpressionBuilder.BuildWhereExpression(searchFilter)}) AND IsDeleted = false";
            }
            else
                sql += $" WHERE IsDeleted = false";

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
            const string sql = "DELETE FROM Modules WHERE Id = @moduleId";

            var parameters = new List<IDataParameter>
            {
                QueryHelper.CreateSqlParameter("@moduleId", Guid.Parse(moduleId), NpgsqlDbType.Uuid)
            };

            return await _queryHelper.ExecuteNonQuery(sql, parameters) == 1;
        }

        private async Task<Module> InsertModule(Module module)
        {
            const string sql = @"INSERT INTO Modules (
                                Name,
                                Description,
                                ParentId,
                                CreatedBy,
                                CreatedDate,
                                UpdatedBy,
                                UpdatedDate,
                                IsDeleted) 
                            VALUES (
                                @name,
                                @description,
                                @parentId, 
                                @createdBy,
                                @createdDate,
                                @updatedBy,
                                @updatedDate,
                                @isDeleted) RETURNING Id;";

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
            const string sql = @"UPDATE Modules
                                SET 
                                    Name = @name,
                                    Description = @description,
                                    ParentId = @parentId,
                                    CreatedBy = @createdBy,
                                    CreatedDate = @createdDate,
                                    UpdatedBy = @updatedBy,
                                    UpdatedDate = @updatedDate,
                                    IsDeleted = @isDeleted
                               WHERE 
                                    Id = @id";

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
            var sql = $@"INSERT INTO Features (
                                ModuleId,
                                Code,
                                Name,
                                Description) 
                            VALUES (
                                {moduleId},
                                @code,
                                @name,
                                @description) RETURNING Id;";

            await _queryHelper.ExecuteScalar(sql, TakeFeature(feature));

            return feature;
        }

        private async Task<Permission> InsertPermission(string moduleId, Permission permission)
        {
            var sql = $@"INSERT INTO Permissions (
                                ModuleId,
                                Code,
                                Name,
                                Description) 
                            VALUES (
                                {moduleId},
                                @code,
                                @name,
                                @description) RETURNING Id;";

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
                QueryHelper.CreateSqlParameter("@id", new Guid(module.Id), NpgsqlDbType.Uuid),
                QueryHelper.CreateSqlParameter("@name", module.Name, NpgsqlDbType.Varchar),
                QueryHelper.CreateSqlParameter("@description", module.Description, NpgsqlDbType.Varchar),
                QueryHelper.CreateSqlParameter("@parentId", module.ParentId, NpgsqlDbType.Varchar),
                QueryHelper.CreateSqlParameter("@isDeleted", module.IsDeleted, NpgsqlDbType.Boolean),
                QueryHelper.CreateSqlParameter("@createdBy", module.CreatedBy, NpgsqlDbType.Varchar),
                QueryHelper.CreateSqlParameter("@createdDate", module.CreatedDate, NpgsqlDbType.Timestamp),
                QueryHelper.CreateSqlParameter("@updatedBy", module.UpdatedBy, NpgsqlDbType.Varchar),
                QueryHelper.CreateSqlParameter("@updatedDate", module.UpdatedDate, NpgsqlDbType.Timestamp)
            };

            return parameters;
        }

        private List<IDataParameter> TakeFeature(Feature feature)
        {
            if (string.IsNullOrEmpty(feature.Id))
                feature.Id = Guid.NewGuid().ToString();

            var parameters = new List<IDataParameter>
            {
                QueryHelper.CreateSqlParameter("@id", new Guid(feature.Id), NpgsqlDbType.Uuid),
                QueryHelper.CreateSqlParameter("@code", feature.Code, NpgsqlDbType.Varchar),
                QueryHelper.CreateSqlParameter("@name", feature.Name, NpgsqlDbType.Varchar),
                QueryHelper.CreateSqlParameter("@description", feature.Description, NpgsqlDbType.Varchar)
            };

            return parameters;
        }

        private List<IDataParameter> TakePermission(Permission permission)
        {
            if (string.IsNullOrEmpty(permission.Id))
                permission.Id = Guid.NewGuid().ToString();

            var parameters = new List<IDataParameter>
            {
                QueryHelper.CreateSqlParameter("@id", new Guid(permission.Id), NpgsqlDbType.Uuid),
                QueryHelper.CreateSqlParameter("@code", permission.Code, NpgsqlDbType.Varchar),
                QueryHelper.CreateSqlParameter("@name", permission.Name, NpgsqlDbType.Varchar),
                QueryHelper.CreateSqlParameter("@description", permission.Description, NpgsqlDbType.Varchar)
            };

            return parameters;
        }

        public Task<Permission> GetPermissionsName(string v)
        {
            throw new NotImplementedException();
        }
    }
}
