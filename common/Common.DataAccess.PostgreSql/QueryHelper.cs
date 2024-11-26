using Common.DataAccess.RDBMS;
using Common.DataAccess.RDBMS.Model;
using Npgsql;
using NpgsqlTypes;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace Common.DataAccess.PostgreSql
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

            using (var connection = new NpgsqlConnection(connectionString))
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

            using (var connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (var command = CreateCommand(sql, connection, sqlParameters))
                using (var sqlDataAdapter = new NpgsqlDataAdapter(command))
                {
                    command.CommandText = sql;
                    command.CommandType = CommandType.Text;
                    await Task.Run(() => sqlDataAdapter.Fill(data));
                }
                connection.Close();
            }

            return data;
        }

        public async Task<IEnumerable<T>> Read<T>(string sql, List<IDataParameter> sqlParameters, Func<IDataReader, T> make)
        {
            using (var connection = new NpgsqlConnection(_connectionString))
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
            using (var connection = new NpgsqlConnection(_connectionString))
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

        private IEnumerable<T> ReadItems<T>(NpgsqlDataReader reader, Func<IDataReader, T> make)
        {
            while (reader.Read())
            {
                yield return make(reader);
            }
        }

        public async Task<object> ExecuteScalar(string sql, List<IDataParameter> sqlParameters = null)
        {
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                using (var command = CreateCommand(sql, connection, sqlParameters))
                {

                    //string query = command.CommandText;

                    //foreach (NpgsqlParameter  p in command.Parameters)
                    //{
                    //    query = query.Replace(p.ParameterName, p.Value.ToString());
                    //}

                    return await command.ExecuteScalarAsync();
                }
            }
        }

        public async Task<int> ExecuteNonQuery(string sql, List<IDataParameter> sqlParameters = null)
        {
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (var command = CreateCommand(sql, connection, sqlParameters))
                {
                    return await command.ExecuteNonQueryAsync();
                }
            }
        }

        public static IDataParameter CreateSqlParameter(string parameterName, object value, NpgsqlDbType type)
        {
            return new NpgsqlParameter
            {
                ParameterName = parameterName,
                Value = value ?? DBNull.Value,
                NpgsqlDbType = type
            };
        }

        private static NpgsqlCommand CreateCommand(string sql, NpgsqlConnection connection, List<IDataParameter> sqlParameters, NpgsqlTransaction transaction = null)
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

        public async void LoadDataIntoSqlServer(DataTable dataTable, string tableName)
        {
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                //    using (var bulkCopy = new NpgsqlBulkCopy(connection))
                //    {
                        await connection.OpenAsync();

                //        bulkCopy.DestinationTableName = tableName;

                //        bulkCopy.WriteToServer(dataTable);
                //    }
            }
        }

        public void LoadDataTable(DataTable dataTable, string tableName)
        {
            throw new NotImplementedException();
        }

        public void LoadCsvData(string csvFilePath, string tableName, List<RDBMS.Model.DbColumn> columns, string separator = ",")
        {
            throw new NotImplementedException();
        }

        public DataTable CsvToDataTable(string csvFilePath, string tableName, List<RDBMS.Model.DbColumn> columns, string separator = ",", int? maxRows = null)
        {
            throw new NotImplementedException();
        }

        public List<DbColumn> GetTableDefinition(string csvFilePath, string separator = ",")
        {
            throw new NotImplementedException();
        }

        public DataTable ExcelToDataTable(string filePath, string tableName, List<RDBMS.Model.DbColumn> columns, int? maxRows = null)
        {
            throw new NotImplementedException();
        }
    }
}