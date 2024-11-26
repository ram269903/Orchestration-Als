using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Common.DataAccess.RDBMS
{
    public interface IRepository<T> where T : IEntity<string>
    {
        Task<IList<T>> GetAllAsync(Expression<Func<T, bool>> filter, List<string> fields = null, string sortProperty = null, SortOrder sortOrder = SortOrder.Descending);
        Task<long> CountAsync(Expression<Func<T, bool>> filter);
        Task<string> CheckNameExists(string name);
        Task<int> DeleteByIdAsync(string id);
    }
}
