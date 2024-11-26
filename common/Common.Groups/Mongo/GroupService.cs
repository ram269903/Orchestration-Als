using Common.Config;
using Common.DataAccess;
using Common.DataAccess.Mongo;
using Common.Groups.Models;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Common.Groups.Mongo
{
    public class GroupService : IGroupService
    {
        private readonly IRepository<Group> _groupsRepository;
        private const string GroupsRepository = "Groups";

        public GroupService(IOptions<DbConfig> dbConfig)
        {
            var databaseConfig = dbConfig.Value;

            var dbSettings = new DbSettings { ConnectionString = databaseConfig.ConnectionString, Database = databaseConfig.Database };

            _groupsRepository = new MongoRepository<Group>(dbSettings, GroupsRepository);
        }

        public GroupService(DbConfig dbConfig)
        {
            var dbSettings = new DbSettings { ConnectionString = dbConfig.ConnectionString, Database = dbConfig.Database };

            _groupsRepository = new MongoRepository<Group>(dbSettings, GroupsRepository);
        }

        public async Task<Group> GetGroup(string groupId)
        {
            if (string.IsNullOrEmpty(groupId)) return null;

            return await _groupsRepository.GetByIdAsync(groupId);
        }

        public async Task<IEnumerable<Group>> GetGroups(string loginUserId, bool isSuperAdmin, SearchFilter searchFilter, string orderBy, SortOrder sortOrder, int? page, int? pageSize, string[] fields = null)
        {
            Expression<Func<Group, bool>> filterExpression = x => true;

            if (searchFilter != null && searchFilter.Filters.Count > 0)
                filterExpression = ExpressionBuilder.GetExpression<Group>(searchFilter);

            IList<Group> groups = null;

            if (page != null && pageSize != null)
                groups = (await _groupsRepository.GetPaginatedAsync(filterExpression, orderBy, sortOrder, (int)page, (int)pageSize)).ToList();
            else
                groups = (await _groupsRepository.GetAllAsync(filterExpression, null, orderBy, sortOrder)).ToList();

            return groups;
        }

        public async Task<IEnumerable<Group>> GetGroups(SearchFilter searchFilter, string orderBy, SortOrder sortOrder, int? page, int? pageSize, string[] fields = null)
        {
            Expression<Func<Group, bool>> filterExpression = x => true;

            if (searchFilter != null && searchFilter.Filters.Count > 0)
                filterExpression = ExpressionBuilder.GetExpression<Group>(searchFilter);

            IList<Group> groups = null;

            if (page != null && pageSize != null)
                groups = (await _groupsRepository.GetPaginatedAsync(filterExpression, orderBy, sortOrder, (int)page, (int)pageSize)).ToList();
            else
                groups = (await _groupsRepository.GetAllAsync(filterExpression, null, orderBy, sortOrder)).ToList();

            return groups;
        }

        public async Task<long> GetGroupsCount(string loginUserId, bool isSuperAdmin, SearchFilter searchFilter = null)
        {
            Expression<Func<Group, bool>> filterExpression = x => true;

            if (searchFilter != null && searchFilter.Filters.Count > 0)
            {
                filterExpression = ExpressionBuilder.GetExpression<Group>(searchFilter);

                //var filter = new Filter<Group>();

                //foreach (var item in searchFilter.Filters)
                //{
                //    filter.By(item.PropertyName, Operation.Contains, item.Value, Connector.And);
                //}

                //filter.By("Name", Operation.Contains, " John");

                //filterExpression = new FilterBuilder().GetExpression<Group>(filter);
            }

            return await _groupsRepository.CountAsync(filterExpression);
        }
        public async Task<bool> DeleteGroupFlag(string groupId)
        {
            ////const string sql = "DELETE FROM [dbo].[Groups] WHERE [Id] = @groupId";
            //const string sql = @"UPDATE [dbo].[Groups] SET [IsDeleted] = 1 WHERE [Id] = @groupId";

            //var parameters = new List<IDataParameter>
            //{
            //    QueryHelper.CreateSqlParameter("@groupId", Guid.Parse(groupId), SqlDbType.UniqueIdentifier)
            //};

            //return await _queryHelper.ExecuteNonQuery(sql, parameters) == 1;f
            return false;
        }
        public async Task<string> CheckNameExists(string name)
        {
            var group = await _groupsRepository.GetOneAsync(x => x.Name == name);

            return group?.Id;
        }

        public async Task<Group> SaveGroup(Group group)
        {
            return await _groupsRepository.UpdateOneAsync(group);
        }

        public async Task<bool> DeleteGroup(string groupId)
        {
            return (await _groupsRepository.DeleteByIdAsync(groupId)) == 1;
        }

    }
}
