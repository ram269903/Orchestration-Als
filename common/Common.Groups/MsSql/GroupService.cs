using Common.Config;
using Common.DataAccess;
using Common.DataAccess.MsSql;
using Common.DataAccess.RDBMS;
using Common.Groups.Models;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace Common.Groups.MsSql
{
    public class GroupService : IGroupService
    {
        private readonly QueryHelper _queryHelper = null;

        public GroupService(IOptions<DbConfig> dbConfig)
        {
            var databaseConfig = dbConfig.Value;

            _queryHelper = new QueryHelper(databaseConfig.ConnectionString);
        }

        public GroupService(DbConfig dbConfig)
        {
            _queryHelper = new QueryHelper(dbConfig.ConnectionString);
        }

        public async Task<Group> GetGroup(string groupId)
        {
            if (string.IsNullOrEmpty(groupId)) return null;

            const string groupSql = @"SELECT * FROM [dbo].[Groups] WHERE [Id] = @groupId AND [IsDeleted] = 0";

            var parameters = new List<IDataParameter>
            {
                QueryHelper.CreateSqlParameter("@groupId", Guid.Parse(groupId), SqlDbType.UniqueIdentifier)
            };

            var group = (await _queryHelper.Read(groupSql, parameters, Make))?.FirstOrDefault();

            if (group == null) return null;

            group.ModuleFeatures = (await GetModuleFeatures(group.Id))?.ToList();

            //foreach (var modFeature in group.ModuleFeatures)
            //{
            //    var module = await _moduleService.GetModule(modFeature.ModuleId);
            //    modFeature.ModuleName = module.Name;
            //}

            return group;

        }

        public async Task<IEnumerable<Group>> GetGroups(string loginUserId, bool isSuperAdmin, SearchFilter searchFilter, string orderBy, SortOrder sortOrder, int? page, int? pageSize, string[] fields = null)
        {
            var columns = "*";

            if (fields!= null && fields.Length > 0)
                columns = string.Join(",", fields);

            string sql = $"SELECT {columns} FROM [dbo].[Groups]";

            if (searchFilter?.Filters?.Count() > 0)
            {
                sql += $" WHERE ({ExpressionBuilder.BuildWhereExpression(searchFilter)}) AND [IsDeleted] = 0";
            }
            else
                sql += $" WHERE [IsDeleted] = 0";

            if (isSuperAdmin == false)
                sql += $" AND [Id] in ( Select ga.[GroupId] from [dbo].[Group_Access] ga where ga.[GroupId] IN (SELECT ug.[GroupId] FROM [dbo].[User_Access] ug WHERE ug.UserId = '{loginUserId}') )";


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

            var groups = (await _queryHelper.Read(sql, null, Make))?.ToList();

            if (groups == null) return null;

            foreach (var group in groups)
            {
                group.ModuleFeatures = (await GetModuleFeatures(group.Id)).ToList();
            }

            return groups;
        }

        public async Task<IEnumerable<Group>> GetGroups(SearchFilter searchFilter, string orderBy, SortOrder sortOrder, int? page, int? pageSize, string[] fields = null)
        {
            var columns = "*";

            if (fields != null && fields.Length > 0)
                columns = string.Join(",", fields);

            string sql = $@"SELECT {columns} FROM [dbo].[Groups]";

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

            var groups = (await _queryHelper.Read(sql, null, Make))?.ToList();

            if (groups == null) return null;

            foreach (var group in groups)
            {
                group.ModuleFeatures = (await GetModuleFeatures(group.Id)).ToList();
            }

            return groups;
        }

        private async Task<IEnumerable<ModuleFeature>> GetModuleFeatures(string groupId) 
        {
            const string sql = @"SELECT * FROM [dbo].[Group_Access] WHERE [GroupId] = @groupId";

            var parameters = new List<IDataParameter>
            {
                QueryHelper.CreateSqlParameter("@groupId", groupId, SqlDbType.NVarChar)
            };

            var tempModuleFeatures = await _queryHelper.Read(sql, parameters, MakeModuleFeature);

            if (tempModuleFeatures == null) return null;

            var moduleFeatures = new List<ModuleFeature>();

            if (tempModuleFeatures != null && tempModuleFeatures.Count() > 0)
            {
                var moduleFeatureGroups = tempModuleFeatures.ToList().GroupBy(x => x.ModuleId);

                foreach (var group in moduleFeatureGroups)
                {
                    var moduleId = group.Key;
                    var featureIds = new List<string>();
                    foreach (var groupedItem in group)
                    {
                        featureIds.Add(groupedItem.FeatureId);
                    }

                    moduleFeatures.Add(new ModuleFeature { ModuleId = moduleId, FeatureIds = featureIds });
                }
            }

            return moduleFeatures;
        }

        public async Task<long> GetGroupsCount(string loginUserId, bool isSuperAdmin, SearchFilter searchFilter = null)
        {
            string sql = @"SELECT COUNT(*) FROM [dbo].[Groups]";

            if (searchFilter?.Filters?.Count() > 0)
            {
                sql += $" WHERE ({ExpressionBuilder.BuildWhereExpression(searchFilter)}) AND [IsDeleted] = 0";
            }
            else
                sql += $" WHERE [IsDeleted] = 0";

            if (isSuperAdmin == false)
                sql += $" AND [Id] in ( Select ga.[GroupId] from [dbo].[Group_Access] ga where ga.[GroupId] IN (SELECT ug.[GroupId] FROM [dbo].[User_Access] ug WHERE ug.UserId = '{loginUserId}') )";

            return Convert.ToInt64(await _queryHelper.ExecuteScalar(sql, null));
        }

        public async Task<string> CheckNameExists(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;

            const string sql = @"SELECT * FROM [dbo].[Groups] WHERE [Name] = @name AND [IsDeleted] = 0";

            var parameters = new List<IDataParameter>
            {
                QueryHelper.CreateSqlParameter("@name", name, SqlDbType.NVarChar)
            };

            var role = (await _queryHelper.Read(sql, parameters, Make)).FirstOrDefault();

            return role?.Id;
        }

        public async Task<Group> SaveGroup(Group group)
        {
            if (string.IsNullOrEmpty(group.Id))
                return await InsertGroup(group);
            else
                return await UpdateGroup(group);
        }
        public async Task<bool> DeleteGroupFlag(string groupId)
        {
            //const string sql = "DELETE FROM [dbo].[Groups] WHERE [Id] = @groupId";
            const string sql = @"UPDATE [dbo].[Groups] SET [IsDeleted] = 1 WHERE [Id] = @groupId";

            var parameters = new List<IDataParameter>
            {
                QueryHelper.CreateSqlParameter("@groupId", Guid.Parse(groupId), SqlDbType.UniqueIdentifier)
            };

            return await _queryHelper.ExecuteNonQuery(sql, parameters) == 1;
        }
        public async Task<bool> DeleteGroup(string groupId)
        {
            const string sql = "DELETE FROM [dbo].[Groups] WHERE [Id] = @groupId";
            //const string sql = @"UPDATE [dbo].[Groups] SET [IsDeleted] = 1 WHERE [Id] = @groupId";

            var parameters = new List<IDataParameter>
            {
                QueryHelper.CreateSqlParameter("@groupId", Guid.Parse(groupId), SqlDbType.UniqueIdentifier)
            };

            return await _queryHelper.ExecuteNonQuery(sql, parameters) == 1;

        }

        private async Task<Group> InsertGroup(Group group)
        {
            const string sql = @"INSERT [dbo].[Groups] (
                                [Name],
                                [Description],
                                [CreatedBy],
                                [CreatedDate],
                                [UpdatedBy],
                                [UpdatedDate],
                                [IsDeleted])
                            OUTPUT Inserted.ID
                            VALUES (
                                @name,
                                @description,
                                @createdBy,
                                @createdDate,
                                @updatedBy,
                                @updatedDate,
                                @isDeleted);";

            var id = await _queryHelper.ExecuteScalar(sql, Take(group));

            group.Id = id.ToString();

            if (group.ModuleFeatures != null && group.ModuleFeatures.Count() > 0)
                UpdateGroupAccess(group);
            
            return group;
        }

        private async Task<Group> UpdateGroup(Group group)
        {
            const string sql = @"UPDATE [dbo].[Groups]
                                SET 
                                    [Name] = @name,
                                    [Description] = @description,
                                    [CreatedBy] = @createdBy,
                                    [CreatedDate] = @createdDate,
                                    [UpdatedBy] = @updatedBy,
                                    [UpdatedDate] = @updatedDate,
                                    [IsDeleted] = @isDeleted
                               WHERE 
                                    [Id] = @groupId";

            _ = await _queryHelper.ExecuteNonQuery(sql, Take(group));

            if (group.ModuleFeatures != null)
                UpdateGroupAccess(group);

            return group;
        }

        private async void UpdateGroupAccess(Group group)
        {
            const string deleteSql = @"DELETE FROM [dbo].[Group_Access] WHERE [GroupId] = @groupId";

            var parameters = new List<IDataParameter>
            {
                QueryHelper.CreateSqlParameter("@groupId", group.Id, SqlDbType.NVarChar)
            };

            _= await _queryHelper.ExecuteNonQuery(deleteSql, parameters) == 1;

            var values = "";

            foreach (var modFeature in group.ModuleFeatures)
            {
                var moduleId = modFeature.ModuleId;

                foreach (var featureId in modFeature.FeatureIds)
                {
                    values += $"('{group.Id}', '{moduleId}', '{featureId}'),";
                }
            }

            if (!string.IsNullOrEmpty(values))
            {
                values = values.Remove(values.Length - 1);

                var sql = $@"INSERT [dbo].[Group_Access] ([GroupId], [ModuleId], [FeatureId]) VALUES {values}";

                _ = await _queryHelper.ExecuteQuery(sql, null);
            }

        }

        private readonly Func<IDataReader, Group> Make = reader =>
            new Group
            {
                Id = reader["Id"].AsString(),
                Name = reader["Name"].AsString(),
                Description = reader["Description"].AsString(),
                IsSuperGroup = reader["IsSuperGroup"].AsBool(),
                IsDeleted = reader["IsDeleted"].AsBool(),
                CreatedBy = reader["CreatedBy"].AsString(),
                CreatedDate = reader.GetNullableDateTime("CreatedDate"),
                UpdatedBy = reader["UpdatedBy"].AsString(),
                UpdatedDate = reader.GetNullableDateTime("UpdatedDate")
            };

        private readonly Func<IDataReader, ModFeature> MakeModuleFeature = reader =>
            new ModFeature
            {
                ModuleId = reader["ModuleId"].AsString(),
                FeatureId = reader["FeatureId"].AsString(),
            };

        private List<IDataParameter> Take(Group group)
        {
            if (string.IsNullOrEmpty(group.Id))
                group.Id = Guid.NewGuid().ToString();

            var parameters = new List<IDataParameter>
            {
                QueryHelper.CreateSqlParameter("@groupId", new Guid(group.Id), SqlDbType.UniqueIdentifier),
                QueryHelper.CreateSqlParameter("@name", group.Name, SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@description", group.Description, SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@isSuperGroup", group.IsSuperGroup == true? 1: 0, SqlDbType.Bit),
                QueryHelper.CreateSqlParameter("@isDeleted", group.IsDeleted == true? 1: 0, SqlDbType.Bit),
                QueryHelper.CreateSqlParameter("@createdBy", group.CreatedBy, SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@createdDate", group.CreatedDate, SqlDbType.DateTime2),
                QueryHelper.CreateSqlParameter("@updatedBy", group.UpdatedBy, SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@updatedDate", group.UpdatedDate, SqlDbType.DateTime2)
            };

            return parameters;
        }
    }
}
