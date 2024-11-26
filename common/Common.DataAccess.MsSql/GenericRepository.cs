using Dapper;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Common.DataAccess.MsSql
{
    public abstract class GenericRepository<T> : IGenericRepository<T> where T : class
    {
        private readonly string _tableName;
        protected GenericRepository(string tableName)
        {
            _tableName = tableName;
        }

        public string ConnectionString
        {
            get; set;
        }

        /// <summary>
        /// Opens a new connection and returns it for use
        /// </summary>
        /// <returns></returns>
        private IDbConnection CreateConnection()
        {
            return new SqlConnection(ConnectionString);
        }

        private IEnumerable<PropertyInfo> GetProperties => typeof(T).GetProperties();
        private async Task<IEnumerable<T>> GetAllAsync()
        {
            using (var connection = CreateConnection())
            {
                return await connection.QueryAsync<T>($"SELECT * FROM {_tableName}");
            }
        }
        private async Task<IEnumerable<T>> GetRecordsBasedOnQuery(string query)
        {
            using (var connection = CreateConnection())
            {
                return await connection.QueryAsync<T>(query);
            }
        }
        public async Task<List<T>> GetAsync(Expression<Func<T, bool>> filter = null, Func<IQueryable<T>, IOrderedQueryable<T>> orderBy = null, params Expression<Func<T, object>>[] includes)
        {
            var data = await GetAllAsync();

            IQueryable<T> query = data.AsQueryable<T>();

            foreach (Expression<Func<T, object>> include in includes)
                query = query.Include(include);

            if (filter != null)
                query = query.Where(filter);

            if (orderBy != null)
                query = orderBy(query);

            return query.ToList();
        }

        public async Task<IList<T>> GetAllAsync(Expression<Func<T, bool>> filter, List<string> fields = null, string sortProperty = null,
            SortOrder sortOrder = SortOrder.Descending)
        {
            var data = await GetAllAsync();

            IQueryable<T> query = data.AsQueryable<T>();

            if (filter != null)
                query = query.Where(filter);

            return query.ToList();
        }

        public async Task<IQueryable<T>> QueryAsync(Expression<Func<T, bool>> filter = null, Func<IQueryable<T>, IOrderedQueryable<T>> orderBy = null)
        {
            var data = await GetAllAsync();

            IQueryable<T> query = data.AsQueryable<T>();

            if (filter != null)
                query = query.Where(filter);

            if (orderBy != null)
                query = orderBy(query);

            return query;
        }
        public async Task<T> GetFirstOrDefaultAsync(Expression<Func<T, bool>> filter = null, params Expression<Func<T, object>>[] includes)
        {
            var data = await GetAllAsync();

            IQueryable<T> query = data.AsQueryable<T>();

            foreach (Expression<Func<T, object>> include in includes)
                query = query.Include(include);

            return query.FirstOrDefault(filter);
        }
        public async Task<T> GetByIdAsync(int id)
        {
            using (var connection = CreateConnection())
            {
                var result = await connection.QueryAsync<T>($"SELECT * FROM {_tableName}");
                return result.ToList<T>().SingleOrDefault();
            }
        }

        public async Task<T> GetByIdAsync(Guid id)
        {
            return await GetAsync(id);
        }
        public async Task<long> GetCountAsync(string tableName, SearchFilter searchFilter = null)
        {
            string sql = $@"SELECT Count(*) FROM [dbo].[{tableName}]";

            if (searchFilter?.Filters?.Count() > 0)
            {
                sql += $" WHERE ({ExpressionBuilder.BuildWhereExpression(searchFilter)}) AND [IsDeleted] = 0";
            }

            using (var connection = CreateConnection())
            {
                return Convert.ToInt64(await connection.ExecuteScalarAsync(sql));
            }
        }
        public async Task<List<T>> GetPagedRecordsAsync(SearchFilter searchFilter, string tableName, string orderBy, SortOrder sortOrder, int? page, int? pageSize)
        {
            string sql = $@"SELECT * FROM [dbo].[{tableName}]";

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

            var data = await GetRecordsBasedOnQuery(sql);

            return data.ToList();
        }
        
		public async Task<List<T>> GetPageAsync(Expression<Func<T, bool>> filter, string sortProperty = null,
            SortOrder sortOrder = SortOrder.Descending, int page = 1, int pageSize = 10)
        {
            var data = await GetAllAsync();

            IQueryable<T> items = data.AsQueryable<T>();

            int itemsCount = items.Count();
            if (itemsCount == 0)
                return default(List<T>);

            return items.Skip(pageSize * (page - 1)).Take(pageSize).ToList();
        }
        public async Task DeleteAsync(Guid id)
        {
            using (var connection = CreateConnection())
            {
                await connection.ExecuteAsync($"DELETE FROM {_tableName} WHERE Id=@Id", new { Id = id });
            }
        }

        public async Task DeleteByIdAsync(int id)
        {
            using (var connection = CreateConnection())
            {
                await connection.ExecuteAsync($"DELETE FROM {_tableName} WHERE Id=@Id", new { Id = id });
            }
        }

        public async Task DeleteByIdAsync(Guid id)
        {
            using (var connection = CreateConnection())
            {
                await connection.ExecuteAsync($"DELETE FROM {_tableName} WHERE Id=@Id", new { Id = id });
            }
        }

        public async Task<T> GetAsync(int id)
        {
            using (var connection = CreateConnection())
            {
                var result = await connection.QuerySingleOrDefaultAsync<T>($"SELECT * FROM {_tableName} WHERE Id=@Id", new { Id = id });
                if (result == null)
                    throw new KeyNotFoundException($"{_tableName} with id [{id}] could not be found.");

                return result;
            }
        }
        public async Task<T> GetAsync(Guid id)
        {
            using (var connection = CreateConnection())
            {
                var result = await connection.QuerySingleOrDefaultAsync<T>($"SELECT * FROM {_tableName} WHERE Id=@Id", new { Id = id });
                if (result == null)
                    throw new KeyNotFoundException($"{_tableName} with id [{id}] could not be found.");

                return result;
            }
        }

        public async Task<int> SaveRangeAsync(IEnumerable<T> list)
        {
            var inserted = 0;
            var query = GenerateInsertQuery();
            using (var connection = CreateConnection())
            {
                inserted += await connection.ExecuteAsync(query, list);
            }

            return inserted;
        }
        public async Task InsertAsync(T t)
        {
            var insertQuery = GenerateInsertQuery(true);

            using (var connection = CreateConnection())
            {
                await connection.ExecuteAsync(insertQuery, t);
            }
        }
        private string GenerateInsertQuery(bool generateQueryForIdColumn = false)
        {
            var insertQuery = new StringBuilder($"INSERT INTO {_tableName} ");

            insertQuery.Append("(");

            var properties = GenerateListOfProperties(GetProperties);

            properties.ForEach(prop =>
            {
                if (generateQueryForIdColumn)
                {
                    if (prop.Equals("Id"))
                        insertQuery.Append($"[{prop}],");
                }

                if (!prop.Equals("Id"))
                {
                    insertQuery.Append($"[{prop}],");
                }
            });

            insertQuery
                .Remove(insertQuery.Length - 1, 1)
                .Append(") VALUES (");

            properties.ForEach(prop =>
            {
                if (generateQueryForIdColumn)
                {
                    if (prop.Equals("Id"))
                        insertQuery.Append($"@{prop},");
                }
                if (!prop.Equals("Id"))
                {
                    insertQuery.Append($"@{prop},");
                }
            });

            insertQuery
                .Remove(insertQuery.Length - 1, 1)
                .Append(")");

            return insertQuery.ToString();
        }

        public async Task UpdateAsync(T t)
        {
            var updateQuery = GenerateUpdateQuery();

            using (var connection = CreateConnection())
            {
                await connection.ExecuteAsync(updateQuery, t);
            }
        }

        public async Task UpdateOneAsync(T t)
        {
            var updateQuery = GenerateUpdateQuery();

            using (var connection = CreateConnection())
            {
                await connection.ExecuteAsync(updateQuery, t);
            }
        }
        private string GenerateUpdateQuery()
        {
            var updateQuery = new StringBuilder($"UPDATE {_tableName} SET ");
            var properties = GenerateListOfProperties(GetProperties);

            properties.ForEach(property =>
            {
                if (!property.Equals("Id"))
                {
                    updateQuery.Append($"{property}=@{property},");
                }
            });

            updateQuery.Remove(updateQuery.Length - 1, 1); //remove last comma
            updateQuery.Append(" WHERE Id=@Id");

            return updateQuery.ToString();
        }
        private static List<string> GenerateListOfProperties(IEnumerable<PropertyInfo> listOfProperties)
        {
            return (from prop in listOfProperties
                    let attributes = prop.GetCustomAttributes(typeof(DescriptionAttribute), false)
                    where attributes.Length <= 0 || (attributes[0] as DescriptionAttribute)?.Description != "ignore"
                    select prop.Name).ToList();
        }
    }
}
