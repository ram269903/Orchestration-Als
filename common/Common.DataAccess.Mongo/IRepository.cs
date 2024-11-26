using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Common.DataAccess.Mongo
{
    public interface IRepository<T> where T : IEntity<string>
    {
        IMongoCollection<T> GetCollection();

        Task<T> GetByIdAsync(string id, List<string> fields = null);

        Task<T> GetOneAsync(Expression<Func<T, bool>> expression, List<string> fields = null);

        IFindFluent<T, T> GetCursor(Expression<Func<T, bool>> filter);

        Task<IList<T>> GetAllAsync(Expression<Func<T, bool>> filter, string[] fields = null, string sortProperty = null, SortOrder sortOrder = SortOrder.Descending);

        Task<long> CountAsync(Expression<Func<T, bool>> filter);

        Task<T> GetByMaxAsync(Expression<Func<T, bool>> filter, Expression<Func<T, object>> orderByDescending);

        Task<T> GetByMinAsync(Expression<Func<T, bool>> filter, Expression<Func<T, object>> orderByAscending);

        Task<IList<T>> GetPaginatedAsync(Expression<Func<T, bool>> filter, string sortProperty = null, SortOrder sortOrder = SortOrder.Descending, int page = 0, int pageSize = 50, string[] fields = null);

        Task<long> DeleteOneAsync(Expression<Func<T, bool>> filter);

        Task<long> DeleteManyAsync(Expression<Func<T, bool>> filter);

        Task<long> DeleteByIdAsync(string id);

        Task<T> UpdateOneAsync(T entity, bool updateDate = true);

        void InsertManyAsync(IEnumerable<T> entities);

        Task<IList<T>> SearchForAsync(BsonDocument[] bsonDocuments);

        Task<IList<T>> SearchForAsync(BsonDocument bsonDocument);

        Task<IList<T>> GetByIdsAsync(IList<string> ids);

    }
}
