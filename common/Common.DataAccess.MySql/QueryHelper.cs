using Common.DataAccess.RDBMS;
using Common.DataAccess.RDBMS.Model;
using System;
using System.Collections.Generic;
using System.Data;
using MySql.Data.MySqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace Common.DataAccess.MySql
{
    public class QueryHelper : IQueryHelper
    {
        private readonly string _connectionString;

        public QueryHelper(string connectionString)
        {
            _connectionString = connectionString;
        }

        public bool IsServerConnected(string connectionString = null)
        {
            if (string.IsNullOrEmpty(connectionString))
                connectionString = _connectionString;

            using (var connection = new MySqlConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    return true;
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }

        public async Task<DataTable> ExecuteQuery(string sql, List<IDataParameter> sqlParameters = null)
        {
            var data = new DataTable();

            using (var connection = new MySqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (var command = CreateCommand(sql, connection, sqlParameters))
                using (var sqlDataAdapter = new MySqlDataAdapter(command))
                {
                    command.CommandText = sql;
                    command.CommandType = CommandType.Text;
                    //sqlDataAdapter.Fill(data);
                    await Task.Run(() => sqlDataAdapter.Fill(data));
                }
                connection.Close();
            }

            return data;
        }

        public async Task<IEnumerable<T>> Read<T>(string sql, List<IDataParameter> sqlParameters, Func<IDataReader, T> make)
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                using (var command = CreateCommand(sql, connection, sqlParameters))
                {
                    await connection.OpenAsync();

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        return ReadItems(reader, make).ToArray();
                    }
                }
            }
        }

        public async Task<IDataReader> Read<T>(string sql, List<IDataParameter> sqlParameters)
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                using (var command = CreateCommand(sql, connection, sqlParameters))
                {
                    await connection.OpenAsync();

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        return reader;
                    }
                }
            }
        }

        private IEnumerable<T> ReadItems<T>(IDataReader reader, Func<IDataReader, T> make)
        {
            while (reader.Read())
            {
                yield return make(reader);
            }
        }

        public async Task<object> ExecuteScalar(string sql, List<IDataParameter> sqlParameters = null)
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                using (var command = CreateCommand(sql, connection, sqlParameters))
                {
                    return await command.ExecuteScalarAsync();
                }
            }
        }

        public async Task<int> ExecuteNonQuery(string sql, List<IDataParameter> sqlParameters = null)
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (var command = CreateCommand(sql, connection, sqlParameters))
                {
                    return await command.ExecuteNonQueryAsync();
                }
            }
        }

        public static IDataParameter CreateSqlParameter(string parameterName, object value, MySqlDbType type, string typeName = "")
        {
            return new MySqlParameter
            {
                ParameterName = parameterName,
                Value = value ?? DBNull.Value,
                MySqlDbType = type
                //TypeName = typeName
            };
        }

        private static MySqlCommand CreateCommand(string sql, MySqlConnection connection, List<IDataParameter> sqlParameters, MySqlTransaction transaction = null)
        {
            var command = connection.CreateCommand();
            //command.CommandTimeout = 0;
            command.CommandType = CommandType.Text;
            command.CommandText = sql;

            if (transaction != null)
                command.Transaction = transaction;

            if (sqlParameters != null)
                command.Parameters.AddRange(sqlParameters.ToArray());

            return command;
        }

        public void LoadDataTable(DataTable dataTable, string tableName)
        {
            throw new NotImplementedException();
        }

        public void LoadCsvData(string csvFilePath, string tableName, List<DbColumn> columns, string separator = ",")
        {
            throw new NotImplementedException();
        }

        public DataTable CsvToDataTable(string csvFilePath, string tableName, List<DbColumn> columns, string separator = ",", int? maxRows = null)
        {
            throw new NotImplementedException();
        }

        public List<DbColumn> GetTableDefinition(string csvFilePath, string separator = ",")
        {
            throw new NotImplementedException();
        }

        public DataTable ExcelToDataTable(string filePath, string tableName, List<DbColumn> columns, int? maxRows = null)
        {
            throw new NotImplementedException();
        }

        //public async void LoadDataIntoSqlServer(DataTable dataTable, string tableName)
        //{
        //using (var connection = new MySqlConnection(_connectionString))
        //{
        //    using (var sqlBulkCopy = new SqlBulkCopy(connection))
        //    {
        //        await connection.OpenAsync();

        //        sqlBulkCopy.DestinationTableName = tableName;

        //        sqlBulkCopy.WriteToServer(dataTable);
        //    }
        //}
        //}



        //public DataTable GetDistinctColumnValues(string tableName, string columnName)
        //{
        //    var sqlQuery = $"SELECT DISTINCT {columnName} FROM [{tableName}] WHERE {columnName} IS NOT NULL";

        //    return ExecuteQuery(sqlQuery, null);
        //}

        //public Task<IEnumerable<T>> Read<T>(ParameterizedQuery pQuery, Func<IDataReader, T> make)
        //{
        //    return Read(pQuery.SqlQuery, pQuery.SqlParameters, make);
        //}

        //public async Task<object> Insert(ParameterizedQuery pQuery)
        //{
        //    return await Insert(pQuery.SqlQuery, pQuery.SqlParameters);
        //}

        //public async Task<object> Insert(string sql, List<SqlParameter> sqlParameters)
        //{
        //    using (var connection = new SqlConnection(_connectionString))
        //    {
        //        await connection.OpenAsync();

        //        using (var command = CreateCommand(sql, connection, sqlParameters))
        //        {
        //            return await command.ExecuteScalarAsync();
        //        }
        //    }
        //}

        //public async Task<int> Update(ParameterizedQuery pQuery)
        //{
        //    return await ExecuteNonQuery(pQuery.SqlQuery, pQuery.SqlParameters);
        //}

        //public async Task<int> Update(string sql, List<SqlParameter> sqlParameters)
        //{
        //    return await ExecuteNonQuery(sql, sqlParameters);
        //}

        //public async Task<int> Delete(ParameterizedQuery pQuery)
        //{
        //    return await ExecuteNonQuery(pQuery.SqlQuery, pQuery.SqlParameters);
        //}

        //public async Task<int> Delete(string sql, List<SqlParameter> sqlParameters)
        //{
        //    return await ExecuteNonQuery(sql, sqlParameters);
        //}



        //public async Task<int> ExecuteNonQuery(ParameterizedQuery pQuery)
        //{
        //    return await ExecuteNonQuery(pQuery.SqlQuery, pQuery.SqlParameters);
        //}

        //public void ExecuteNonQuery(List<ParameterizedQuery> pQueries, bool inTransaction)
        //{
        //    using (var connection = new SqlConnection(_connectionString))
        //    {
        //        connection.Open();

        //        if (inTransaction)
        //        {
        //            using (var transaction = connection.BeginTransaction())
        //            {
        //                try
        //                {
        //                    ExecuteNonQuery(connection, pQueries, transaction);

        //                    transaction.Commit();
        //                }
        //                catch
        //                {
        //                    transaction.Rollback();
        //                    connection.Close();
        //                    throw;
        //                }
        //            }
        //        }
        //        else
        //        {
        //            ExecuteNonQuery(connection, pQueries);
        //        }

        //        connection.Close();
        //    }
        //}

        //public void ExportToCsv(string file, string separator, bool includeHeaders, ParameterizedQuery pQuery)
        //{
        //    using (var connection = new SqlConnection(_connectionString))
        //    {
        //        connection.Open();

        //        using (var command = CreateCommand(pQuery.SqlQuery, connection, pQuery.SqlParameters))
        //        {
        //            using (var reader = command.ExecuteReader())
        //            {
        //                CsvUtil.GenerateCsvFile(reader, file, separator, includeHeaders);
        //            }
        //        }

        //        connection.Close();
        //    }
        //}

        //public void ExportToExcel(string file, ParameterizedQuery pQuery)
        //{
        //    using (var connection = new SqlConnection(_connectionString))
        //    {
        //        connection.Open();

        //        using (var command = CreateCommand(pQuery.SqlQuery, connection, pQuery.SqlParameters))
        //        {
        //            using (var reader = command.ExecuteReader())
        //            {
        //                ExcelUtil.GenerateExcel(reader, file);
        //            }
        //        }

        //        connection.Close();
        //    }
        //}

        //private static void ExecuteNonQuery(SqlConnection connection, IEnumerable<ParameterizedQuery> pQueries, SqlTransaction transaction = null)
        //{
        //    foreach (var pQuery in pQueries)
        //    {
        //        using (var command = CreateCommand(pQuery.SqlQuery, connection, pQuery.SqlParameters, transaction))
        //        {
        //            command.ExecuteNonQuery();
        //            command.Parameters.Clear();
        //        }
        //    }
        //}

    }
}