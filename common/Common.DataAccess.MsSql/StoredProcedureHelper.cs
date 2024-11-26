//https://www.oreilly.com/library/view/adonet-in-a/0596003617/ch04s05.html

using Common.DataAccess.RDBMS;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace Common.DataAccess.MsSql
{
    public class StoredProcedureHelper : IStoredProcedureHelper
    {
        private readonly string _connectionString;

        public StoredProcedureHelper(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<DataTable> ExecuteQuery(string storedProcedureName, List<IDataParameter> sqlParameters = null)
        {
            var data = new DataTable();

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (var command = CreateCommand(storedProcedureName, connection, sqlParameters))
                using (var sqlDataAdapter = new SqlDataAdapter(command))
                {
                    command.CommandText = storedProcedureName;
                    command.CommandType = CommandType.StoredProcedure;
                    await Task.Run(() => sqlDataAdapter.Fill(data));
                }

                connection.Close();
            }

            return data;

        }

        public async Task<IEnumerable<T>> Read<T>(string storedProcedureName, List<IDataParameter> sqlParameters, Func<IDataReader, T> make)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                using (var command = CreateCommand(storedProcedureName, connection, sqlParameters))
                {
                    await connection.OpenAsync();

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        return ReadItems(reader, make).ToArray();
                    }
                }

                //connection.Close();

            }
        }

        public async Task<object> ExecuteScalar(string storedProcedureName, List<IDataParameter> sqlParameters = null)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                using (var command = CreateCommand(storedProcedureName, connection, sqlParameters))
                {
                    return await command.ExecuteScalarAsync();
                }
            }
        }

        public async Task<int> ExecuteNonQuery(string storedProcedureName, List<IDataParameter> sqlParameters = null)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (var command = CreateCommand(storedProcedureName, connection, sqlParameters))
                {
                    return await command.ExecuteNonQueryAsync();
                }
            }
        }

        public static IDataParameter CreateSqlParameter(string parameterName, object value, SqlDbType type, ParameterDirection direction = ParameterDirection.Input, string typeName = "")
        {
            return new SqlParameter
            {
                ParameterName = parameterName,
                Value = value ?? DBNull.Value,
                SqlDbType = type,
                Direction = direction,
                TypeName = typeName
            };
        }

        private static SqlCommand CreateCommand(string storedProcedureName, SqlConnection connection, List<IDataParameter> sqlParameters, SqlTransaction transaction = null)
        {
            var command = connection.CreateCommand();
            //command.CommandTimeout = 0;
            command.CommandType = CommandType.StoredProcedure;
            command.CommandText = storedProcedureName;

            if (transaction != null)
                command.Transaction = transaction;

            if (sqlParameters != null)
                command.Parameters.AddRange(sqlParameters.ToArray());

            return command;
        }

        private IEnumerable<T> ReadItems<T>(SqlDataReader reader, Func<IDataReader, T> make)
        {
            while (reader.Read())
            {
                yield return make(reader);
            }
        }

    }
}
