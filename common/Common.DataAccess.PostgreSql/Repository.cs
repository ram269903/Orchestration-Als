//using Common.Config;
//using Common.DataAccess.Interfaces;
//using Common.DataAccess;
//using Common.DataAccess.RDBMS;
//using NpgsqlTypes;
//using System;
//using System.Collections.Generic;
//using System.Data;
//using System.Linq.Expressions;
//using System.Reflection;
//using System.Threading.Tasks;

//namespace Common.DataAccess.PostgreSql
//{
//    public class Repository<T> : IRepository<T> where T : IEntity<string>
//    {
//        private readonly QueryHelper _queryHelper = null;
//        private readonly string _tableName;

//        protected Repository(DbConfig dbConfig, string tableName)
//        {
//            var databaseConfig = dbConfig;
//            _tableName = tableName;

//            _queryHelper = new QueryHelper(databaseConfig.ConnectionString);
//        }

//        public async Task<string> CheckNameExists(string name)
//        {
//            if (string.IsNullOrEmpty(name)) return null;

//            var sql = $"SELECT * FROM {_tableName} WHERE Name = @name";

//            var parameters = new List<IDataParameter>
//            {
//                QueryHelper.CreateSqlParameter("@name", name, NpgsqlDbType.Varchar)
//            };

//            var id = await _queryHelper.ExecuteScalar(sql, parameters);

//            return id.ToString();
//        }

//        public async Task<long> CountAsync(Expression<Func<T, bool>> filter)
//        {
//            var sql = $"SELECT COUNT(*) FROM {_tableName}";

//            var queryTranslator = new QueryTranslator();

//            sql += " WHERE " + queryTranslator.Translate(filter);

//            return Convert.ToInt64(await _queryHelper.ExecuteScalar(sql, null));
//        }

//        public async Task<int> DeleteByIdAsync(string id)
//        {
//            var sql = $"DELETE FROM {_tableName} WHERE Id = @id";

//            var parameters = new List<IDataParameter>
//            {
//                QueryHelper.CreateSqlParameter("@id", id, NpgsqlDbType.Varchar)
//            };

//            return await _queryHelper.ExecuteNonQuery(sql, parameters);
//        }

//        public async Task<IList<T>> GetAllAsync(Expression<Func<T, bool>> filter, List<string> fields = null, string sortProperty = null, SortOrder sortOrder = null)
//        {
//            var sql = $"SELECT * FROM {_tableName}";

//            var queryTranslator = new QueryTranslator();

//            sql += " WHERE " + queryTranslator.Translate(filter);

//            return (await _queryHelper.Read(sql, null, Make)).ToList();
//        }

//        public Task<IList<T>> GetAllAsync(Expression<Func<T, bool>> filter, List<string> fields = null, string sortProperty = null, SortOrder sortOrder = null)
//        {
//            throw new NotImplementedException();
//        }

//        //private readonly Func<IDataReader, T> Make = reader =>
//        //    new Role
//        //    {
//        //        Id = reader["Id"].AsString(),
//        //        Name = reader["Name"].AsString(),
//        //        Description = reader["Description"].AsString(),
//        //        IsDeleted = reader["IsDeleted"].AsBool(),
//        //        CreatedBy = reader["CreatedBy"].AsString(),
//        //        CreatedDate = reader.GetNullableDateTime("CreatedDate"),
//        //        UpdatedBy = reader["UpdatedBy"].AsString(),
//        //        UpdatedDate = reader.GetNullableDateTime("UpdatedDate")
//        //    };


//    }
//}
