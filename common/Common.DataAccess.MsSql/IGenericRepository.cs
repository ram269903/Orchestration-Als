using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Common.DataAccess.MsSql
{

    public interface IGenericRepository<T>
    {
        //Task<IEnumerable<T>> GetAllAsync();
        Task<List<T>> GetAsync(
        Expression<Func<T, bool>> filter = null,
        Func<IQueryable<T>, IOrderedQueryable<T>> orderBy = null,
        params Expression<Func<T, object>>[] includes);

        Task<T> GetFirstOrDefaultAsync(
        Expression<Func<T, bool>> filter = null,
        params Expression<Func<T, object>>[] includes);
        Task<T> GetAsync(int id);
        Task<T> GetAsync(Guid id);
        Task<long> GetCountAsync(string tableName, SearchFilter searchFilter = null);
        Task<IQueryable<T>> QueryAsync(Expression<Func<T, bool>> filter = null, Func<IQueryable<T>, IOrderedQueryable<T>> orderBy = null);
        Task<List<T>> GetPageAsync(Expression<Func<T, bool>> filter, string sortProperty = null,
            SortOrder sortOrder = SortOrder.Descending, int page = 1, int pageSize = 10);
        Task<T> GetByIdAsync(int id);
        Task<T> GetByIdAsync(Guid id);
        Task DeleteAsync(Guid id);
        Task DeleteByIdAsync(int id);
        Task DeleteByIdAsync(Guid id);
        Task<int> SaveRangeAsync(IEnumerable<T> list);
        Task UpdateAsync(T t);
        Task UpdateOneAsync(T t);
        Task InsertAsync(T t);
    }
}
