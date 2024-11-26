using MongoDB.Bson;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Common.DataAccess.Mongo
{
    public class MongoRepository<T> : IRepository<T> where T : IEntity<string>
    {
        private IMongoDatabase _database;
        private IMongoCollection<T> _collection;

        protected IMongoCollection<T> Collection
        {
            get { return _collection; }
        }

        public IMongoCollection<T> GetCollection()
        {
            return _collection;
        }

        public IQueryable<T> Query
        {
            get { return _collection.AsQueryable<T>(); }
            set { Query = value; }
        }

        public MongoRepository(IDbSettings settings, string collection)
        {
            ConventionRegistry.Register("Ignore null values",
                new ConventionPack {
                    new IgnoreIfNullConvention(true)
                }, 
                t => true);

            var client = new MongoClient(settings.ConnectionString);

            _database = client.GetDatabase(settings.Database);
            _collection = _database.GetCollection<T>(collection);
        }

        /// <summary>
        /// Asynchronously returns one document given its id.
        /// </summary>
        /// <typeparam name="T">The type representing a Document.</typeparam>
        /// <param name="id">The Id of the document you want to get.</param>
        /// <param name="fields">List of fields to return.</param>
        public virtual async Task<T> GetByIdAsync(string id, List<string> fields = null)
        {
            if (fields == null)
                return await _collection.Find(e => e.Id == id).FirstOrDefaultAsync();
            else
            {
                var projectionBuilder = Builders<T>.Projection;
                var projection = projectionBuilder.Combine(fields.Select(field => projectionBuilder.Include(field)));

                return await _collection.Find(e => e.Id == id).Project<T>(projection).FirstOrDefaultAsync();
            }
        }

        /// <summary>
        /// Asynchronously returns one document given an expression filter.
        /// </summary>
        /// <typeparam name="T">The type representing a Document.</typeparam>
        /// <param name="filter">A LINQ expression filter.</param>
        /// <param name="fields">List of fields to return.</param>
        public virtual async Task<T> GetOneAsync(Expression<Func<T, bool>> filter, List<string> fields = null)
        {
            if (fields == null)
                return await _collection.Find(filter).FirstOrDefaultAsync();
            else
            {
                var projectionBuilder = Builders<T>.Projection;
                var projection = projectionBuilder.Combine(fields.Select(field => projectionBuilder.Include(field)));

                return await _collection.Find(filter).Project<T>(projection).FirstOrDefaultAsync();
            }
        }

        /// <summary>
        /// Returns a collection cursor.
        /// </summary>
        /// <typeparam name="T">The type representing a Document.</typeparam>
        /// <param name="filter">A LINQ expression filter.</param>
        public virtual IFindFluent<T, T> GetCursor(Expression<Func<T, bool>> filter)
        {
            return _collection.Find(filter);
        }

        /// <summary>
        /// Asynchronously returns a list of the documents matching the filter condition.
        /// </summary>
        /// <typeparam name="T">The type representing a Document.</typeparam>
        /// <param name="filter">A LINQ expression filter.</param>
        /// <param name="fields">List of fields to return.</param>
        /// <param name="sortProperty">Sort by field.</param>
        /// <param name="sortOrder">Sort Order (Ascending/Descending).</param>
        public virtual async Task<IList<T>> GetAllAsync(Expression<Func<T, bool>> filter, string[] fields = null, string sortProperty = null, SortOrder sortOrder = SortOrder.Descending)
        {
            if (fields == null || fields.Length == 0)
            {
                if (string.IsNullOrEmpty(sortProperty))
                    return await _collection.Find(filter).ToListAsync();
                else
                {
                    var sortExpression = new BsonDocument(sortProperty, (int)sortOrder);
                    //var options = new FindOptions<BsonDocument>
                    //{
                    //    Sort = sortExpression
                    //};
                    return await _collection.Find(filter).Sort(sortExpression).ToListAsync();
                }
            }
            else
            {
                var projectionBuilder = Builders<T>.Projection;
                var projection = projectionBuilder.Combine(fields.Select(field => projectionBuilder.Include(field)));

                if (string.IsNullOrEmpty(sortProperty))
                    return await _collection.Find(filter).Project<T>(projection).ToListAsync();
                else
                {
                    var sortExpression = new BsonDocument(sortProperty, (int)sortOrder);
                    //var options = new FindOptions<BsonDocument>
                    //{
                    //    Sort = sortExpression
                    //};
                    return await _collection.Find(filter).Project<T>(projection).Sort(sortExpression).ToListAsync();
                }
            }
        }

        /// <summary>
        /// Asynchronously counts how many documents match the filter condition.
        /// </summary>
        /// <typeparam name="T">The type representing a Document.</typeparam>
        /// <param name="filter">A LINQ expression filter.</param>
        public virtual async Task<long> CountAsync(Expression<Func<T, bool>> filter)
        {
            return await _collection.CountDocumentsAsync(filter);
        }

        /// <summary>
        /// Gets the document with the maximum value of a specified property in a MongoDB collections that is satisfying the filter.
        /// </summary>
        /// <typeparam name="TDocument">The document type.</typeparam>
        /// <param name="filter">A LINQ expression filter.</param>
        /// <param name="orderByDescending">A property selector to order by descending.</param>
        public virtual async Task<T> GetByMaxAsync(Expression<Func<T, bool>> filter, Expression<Func<T, object>> orderByDescending)
        {
            return await _collection.Find(filter).SortByDescending(orderByDescending).FirstOrDefaultAsync();
        }

        /// <summary>
        /// Gets the document with the maximum value of a specified property in a MongoDB collections that is satisfying the filter.
        /// </summary>
        /// <typeparam name="">The document type.</typeparam>
        /// <param name="filter">A LINQ expression filter.</param>
        /// <param name="orderByAscending">A property selector to order by ascending.</param>
        public virtual async Task<T> GetByMinAsync(Expression<Func<T, bool>> filter, Expression<Func<T, object>> orderByAscending)
        {
            return await _collection.Find(filter).SortBy(orderByAscending).FirstOrDefaultAsync();
        }

        /// <summary>
        /// Asynchronously returns a paginated list of the documents matching the filter condition.
        /// </summary>
        /// <typeparam name="T">The type representing a Document.</typeparam>
        /// <param name="filter"></param>
        /// <param name="sortProperty">Sort by field.</param>
        /// <param name="sortOrder">Sort Order (Ascending/Descending).</param>
        /// <param name="page">The Page number. Default value is 0.</param>
        /// <param name="pageSize">The number of documents you want to take. Default value is 50.</param>
        public virtual async Task<IList<T>> GetPaginatedAsync(Expression<Func<T, bool>> filter, string sortProperty = null, SortOrder sortOrder = SortOrder.Descending, int page = 0, int pageSize = 50, string[] fields = null)
        {
            ProjectionDefinition<T> projection = null;

            if (fields != null && fields.Length > 0)
            {
                //var tempFields = fields.Split(',').ToList();
                var projectionBuilder = Builders<T>.Projection;
                projection = projectionBuilder.Combine(fields.Select(field => projectionBuilder.Include(field)));
            }
            
            if (sortProperty == null)
                
                if (projection == null)
                    return await _collection.Find(filter).Skip((page - 1) * pageSize).Limit(pageSize).ToListAsync();
                else
                    return await _collection.Find(filter).Project<T>(projection).Skip((page - 1) * pageSize).Limit(pageSize).ToListAsync();
            else
            {
                var sortExpression = new BsonDocument(sortProperty, (int)sortOrder);
                //var options = new FindOptions<BsonDocument>
                //{
                //    Sort = sortExpression
                //};
                if (projection == null)
                    return await _collection.Find(filter).Sort(sortExpression).Skip((page - 1) * pageSize).Limit(pageSize).ToListAsync();
                else
                    return await _collection.Find(filter).Sort(sortExpression).Project<T>(projection).Skip((page - 1) * pageSize).Limit(pageSize).ToListAsync();
            }
        }

        /// <summary>
        /// Asynchronously deletes a document matching the condition of the LINQ expression filter.
        /// </summary>
        /// <typeparam name="T">The type representing a Document.</typeparam>
        /// <param name="filter">A LINQ expression filter.</param>
        /// <returns>The number of documents deleted.</returns>
        public virtual async Task<long> DeleteOneAsync(Expression<Func<T, bool>> filter)
        {
            return (await _collection.DeleteOneAsync(filter)).DeletedCount;
        }

        /// <summary>
        /// Asynchronously deletes the documents matching the condition of the LINQ expression filter.
        /// </summary>
        /// <typeparam name="T">The type representing a Document.</typeparam>
        /// <param name="filter">A LINQ expression filter.</param>
        /// <returns>The number of documents deleted.</returns>
        public virtual async Task<long> DeleteManyAsync(Expression<Func<T, bool>> filter)
        {
            return (await _collection.DeleteManyAsync(filter)).DeletedCount;
        }

        /// <summary>
        /// Asynchronously deletes the document matching the document Id.
        /// </summary>
        /// <typeparam name="T">The type representing a Document.</typeparam>
        /// <param name="id">Document Id.</param>
        /// <returns>The number of documents deleted.</returns>
        public virtual async Task<long> DeleteByIdAsync(string id)
        {
            return (await _collection.DeleteOneAsync(e => e.Id == id)).DeletedCount;
        }

        /// <summary>
        /// Takes a document you want to modify and applies the update you have defined in MongoDb.
        /// </summary>
        /// <typeparam name="T">The type representing a Document.</typeparam>
        /// <param name="entity">The document you want to Add or update.</param>
        /// <param name="updateDate">The update the update date of the document.</param>
        public virtual async Task<T> UpdateOneAsync(T entity, bool updateDate = true)
        {
            if (string.IsNullOrEmpty(entity.Id))
            {
                entity.Id = ObjectId.GenerateNewId().ToString();
                entity.CreatedDate = DateTime.Now;
            }

            if (updateDate)
                entity.UpdatedDate = DateTime.Now;

            var filter = Builders<T>.Filter.Eq(s => s.Id, entity.Id);
            await _collection.ReplaceOneAsync(filter, entity, new ReplaceOptions { IsUpsert = true });

            return entity;
        }

        public virtual async void InsertManyAsync(IEnumerable<T> entities)
        {
            await _collection.InsertManyAsync(entities);
        }

        public virtual async Task<IList<T>> SearchForAsync(BsonDocument[] bsonDocuments)
        {
            return await _collection.Aggregate<T>(bsonDocuments).ToListAsync(); 
        }

        public virtual async Task<IList<T>> SearchForAsync(BsonDocument bsonDocument)
        {
            return await _collection.Find(bsonDocument).SortByDescending(x => x.UpdatedDate).ToListAsync();
        }

        public virtual async Task<IList<T>> GetByIdsAsync(IList<string> ids)
        {
            var filter = Builders<T>.Filter.In(p => p.Id, ids);

            return await _collection.Find(filter).ToListAsync();
        }
    }
}