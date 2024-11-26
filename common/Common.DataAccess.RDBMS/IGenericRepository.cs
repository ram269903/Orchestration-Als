namespace Common.DataAccess.RDBMS
{

    public interface IGenericRepository<T>
    {
        ////Task<IEnumerable<T>> GetAllAsync();
        //Task<List<T>> GetAsync(
        //Expression<Func<T, bool>> filter = null,
        //Func<IQueryable<T>, IOrderedQueryable<T>> orderBy = null,
        //params Expression<Func<T, object>>[] includes);

        //Task<T> GetFirstOrDefaultAsync(
        //Expression<Func<T, bool>> filter = null,
        //params Expression<Func<T, object>>[] includes);
        //Task<T> GetAsync(int id);
        //Task<T> GetAsync(Guid id);
        //Task<long> CountAsync(Expression<Func<T, bool>> filter);
        //Task<IQueryable<T>> QueryAsync(Expression<Func<T, bool>> filter = null, Func<IQueryable<T>, IOrderedQueryable<T>> orderBy = null);
        //Task<List<T>> GetPageAsync(Expression<Func<T, bool>> filter, string sortProperty = null,
        //    Common.DataAccess.Models.SortOrder sortOrder = Common.DataAccess.Models.SortOrder.Descending, int page = 1, int pageSize = 10);
        //Task<T> GetByIdAsync(int id);
        //Task<T> GetByIdAsync(Guid id);
        //Task DeleteAsync(Guid id);
        //Task DeleteByIdAsync(int id);
        //Task DeleteByIdAsync(Guid id);
        //Task<int> SaveRangeAsync(IEnumerable<T> list);
        //Task UpdateAsync(T t);
        //Task UpdateOneAsync(T t);
        //Task InsertAsync(T t);
    }
}
