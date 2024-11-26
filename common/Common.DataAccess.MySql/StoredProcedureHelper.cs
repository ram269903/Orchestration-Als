//https://www.oreilly.com/library/view/adonet-in-a/0596003617/ch04s05.html

using Common.DataAccess.RDBMS;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace Common.DataAccess.MySql
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

            using (var connection = new MySqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (var command = CreateCommand(storedProcedureName, connection, sqlParameters))
                using (var sqlDataAdapter = new MySqlDataAdapter(command))
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
            using (var connection = new MySqlConnection(_connectionString))
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
            using (var connection = new MySqlConnection(_connectionString))
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
            using (var connection = new MySqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (var command = CreateCommand(storedProcedureName, connection, sqlParameters))
                {
                    return await command.ExecuteNonQueryAsync();
                }
            }
        }

        public static IDataParameter CreateSqlParameter(string parameterName, object value, MySqlDbType type, ParameterDirection direction = ParameterDirection.Input, string typeName = "")
        {
            return new MySqlParameter
            {
                ParameterName = parameterName,
                Value = value ?? DBNull.Value,
                MySqlDbType = type,
                Direction = direction,
                //TypeName = typeName
            };
        }

        private static MySqlCommand CreateCommand(string storedProcedureName, MySqlConnection connection, List<IDataParameter> sqlParameters, MySqlTransaction transaction = null)
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

        private IEnumerable<T> ReadItems<T>(IDataReader reader, Func<IDataReader, T> make)
        {
            while (reader.Read())
            {
                yield return make(reader);
            }
        }

    }
}
