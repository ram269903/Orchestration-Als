using Common.DataAccess.RDBMS;
using Common.DataAccess.RDBMS.Model;
using Serilog;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Common.DataAccess.MsSql
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

            using (var connection = new SqlConnection(connectionString))
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

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (var command = CreateCommand(sql, connection, sqlParameters))
                using (var sqlDataAdapter = new SqlDataAdapter(command))
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
            using (var connection = new SqlConnection(_connectionString))
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
            using (var connection = new SqlConnection(_connectionString))
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

        private IEnumerable<T> ReadItems<T>(SqlDataReader reader, Func<IDataReader, T> make)
        {
            while (reader.Read())
            {
                yield return make(reader);
            }
        }

        public async Task<object> ExecuteScalar(string sql, List<IDataParameter> sqlParameters = null)
        {
            using (var connection = new SqlConnection(_connectionString))
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
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (var command = CreateCommand(sql, connection, sqlParameters))
                {
                    return await command.ExecuteNonQueryAsync();
                }
            }
        }

        public static IDataParameter CreateSqlParameter(string parameterName, object value, SqlDbType type, string typeName = "")
        {
            return new SqlParameter
            {
                ParameterName = parameterName,
                Value = value ?? DBNull.Value,
                SqlDbType = type,
                TypeName = typeName
            };
        }

        private static SqlCommand CreateCommand(string sql, SqlConnection connection, List<IDataParameter> sqlParameters, SqlTransaction transaction = null)
        {
            var command = connection.CreateCommand();
            command.CommandTimeout = 300;
            command.CommandType = CommandType.Text;
            command.CommandText = sql;

            if (transaction != null)
                command.Transaction = transaction;

            if (sqlParameters != null)
                command.Parameters.AddRange(sqlParameters.ToArray());

            return command;
        }

        public bool LoadDataTable(DataTable dataTable, string tableName)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    using (var sqlBulkCopy = new SqlBulkCopy(connection, SqlBulkCopyOptions.TableLock, null))
                    {
                        connection.Open();
                        sqlBulkCopy.BulkCopyTimeout = 600;
                        sqlBulkCopy.BatchSize = 1000;

                        sqlBulkCopy.DestinationTableName = $"[{tableName}]";

                        sqlBulkCopy.WriteToServer(dataTable);

                        connection.Close();
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error in Sql bulk");
                return false;
            }
        }


        public bool LoadCsvData(string csvFilePath, string tableName, List<DbColumn> columns, string separator = ",")
        {
            try
            {
                var dataTable = CsvToDataTable(csvFilePath, tableName, columns, separator);

                var status = LoadDataTable(dataTable, tableName);

                return status;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error: ");
                return false;
            }
        }

        public DataTable CsvToDataTable(string csvFilePath, string tableName, List<DbColumn> columns, string separator = ",", int? maxRows = null)
        {
            try
            {
                var dataTable = new DataTable(tableName);

                using (var stream = File.OpenRead(csvFilePath))
                {
                    using (var streamReader = new StreamReader(stream))
                    {
                        //Skip header
                        streamReader.ReadLine();

                        foreach (var column in columns)
                        {
                            dataTable.Columns.Add(column.ColumnName, GetNativeDataType(column.DataType));
                        }

                        int count = 0;

                        while (!streamReader.EndOfStream)
                        {
                            if (maxRows != null && count > maxRows.Value - 1)
                                break;

                            var line = streamReader.ReadLine();

                            if (string.IsNullOrEmpty(line))
                                continue;

                            //string[] rowData = Regex.Split(line, separator + "(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)");

                            string[] rowData = line.Split(separator);

                            DataRow dr = dataTable.NewRow();

                            for (int i = 0; i < columns.Count; i++)
                            {
                                var value = Convert.ToString(rowData[i]).Trim();


                                dr[i] = ConvertDataType(value, columns[i].DataType);
                            }

                            dataTable.Rows.Add(dr);

                            count += 1;
                        }
                    }
                }

                return dataTable;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "CsvToDataTable");
                return null;

            }
        }

        public DataTable ExcelToDataTable(string filePath, string tableName, List<DbColumn> columns, int? maxRows = null)
        {
            var dataTable = new DataTable(tableName);

            using (var pck = new OfficeOpenXml.ExcelPackage())
            {
                using (var stream = System.IO.File.OpenRead(filePath))
                {
                    pck.Load(stream);
                }

                var ws = pck.Workbook.Worksheets.First();

                foreach (var column in columns)
                {
                    dataTable.Columns.Add(column.ColumnName, GetNativeDataType(column.DataType));
                }

                var startRow = 2;

                var rowCount = (maxRows != null) ? maxRows.Value + 1 : ws.Dimension.End.Row;

                for (int rowNum = startRow; rowNum <= rowCount; rowNum++)
                {
                    var wsRow = ws.Cells[rowNum, 1, rowNum, ws.Dimension.End.Column];
                    DataRow row = dataTable.Rows.Add();
                    foreach (var cell in wsRow)
                    {
                        var value = Convert.ToString(cell.Text)?.Trim();
                        try
                        {
                            row[cell.Start.Column - 1] = ConvertDataType(value, columns[cell.Start.Column - 1].DataType);
                        }
                        catch (Exception)
                        {
                            row[cell.Start.Column - 1] = DBNull.Value;
                        }

                    }
                }

                return dataTable;
            }
        }

        public List<DbColumn> GetTableDefinition(string filePath, string separator = ",")
        {
            var extension = Path.GetExtension(filePath);
            List<DbColumn> columns = null;

            if (extension != null)
            {
                if (extension.ToLower() == ".csv")
                    columns = GetTableDefinitionFromCsv(filePath, separator);
                else
                    columns = GetTableDefinitionFromExcel(filePath);
            }

            return columns;
        }

        public List<DbColumn> GetTableDefinitionFromCsv(string csvFilePath, string separator = ",")
        {
            var columns = new List<DbColumn>();

            string headerLine, dataLine;

            using (var reader = new StreamReader(csvFilePath))
            {
                headerLine = reader.ReadLine();
                dataLine = reader.ReadLine();
            }

            string[] headers = Regex.Split(headerLine, separator + "(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)");
            string[] rowData = Regex.Split(dataLine, separator + "(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)");

            for (int i = 0; i < headers.Length; i++)
            {
                var name = headers[i].Trim();
                var dataType = GetDataType(rowData[i].ToString());
                var size = GetDefaultSize(dataType);
                columns.Add(new DbColumn { ColumnName = name, DataType = dataType, Size = size, IsNullable = true });
            }

            return columns;
        }

        public List<DbColumn> GetTableDefinitionFromExcel(string filePath)
        {
            using (var pck = new OfficeOpenXml.ExcelPackage())
            {
                using (var stream = System.IO.File.OpenRead(filePath))
                {
                    pck.Load(stream);
                }

                var ws = pck.Workbook.Worksheets.First();

                var columns = new List<DbColumn>();

                var wsRow = ws.Cells[2, 1, 2, ws.Dimension.End.Column];
                var colIndex = 1;

                foreach (var firstRowCell in ws.Cells[1, 1, 1, ws.Dimension.End.Column])
                {
                    var dataType = GetDataType(wsRow[2, colIndex].Text);

                    //GetDataType(wsRow[1,firstRowCell.Text]).ToString());
                    var size = GetDefaultSize(dataType);

                    columns.Add(new DbColumn { ColumnName = firstRowCell.Text, DataType = dataType, Size = size, IsNullable = true });
                    colIndex += 1;
                }

                //var startRow = 2;

                //var rowCount = (maxRows != null) ? maxRows.Value + 1 : ws.Dimension.End.Row;

                //for (int rowNum = startRow; rowNum <= rowCount; rowNum++)
                //{
                //    var wsRow = ws.Cells[rowNum, 1, rowNum, ws.Dimension.End.Column];
                //    DataRow row = dataTable.Rows.Add();
                //    foreach (var cell in wsRow)
                //    {
                //        row[cell.Start.Column - 1] = cell.Text;
                //    }
                //}

                return columns;
            }
        }

        private Type GetNativeDataType(string dataType)
        {

            switch (dataType.ToLower())
            {
                case "bool":
                    return typeof(bool);
                case "int":
                case "int32":
                    return typeof(int);
                case "int64":
                case "long":
                    return typeof(long);
                case "double":
                    return typeof(double);
                case "datetime":
                case "datetime2":
                    return typeof(DateTime);
                case "guid":
                    return typeof(Guid);
                default:
                    return typeof(string);
            }
        }

        private string GetDataType(string str)
        {
            if (bool.TryParse(str, out _))
                return "bool";
            else if (int.TryParse(str, out _))
                return "int32";
            else if (long.TryParse(str, out _))
                return "int64";
            else if (double.TryParse(str, out _))
                return "double";
            else if (DateTime.TryParse(str, out _))
                return "dateTime";
            else return "string";

        }

        private object ConvertDataType(string value, string dataType)
        {
            if (string.IsNullOrEmpty(value)) return DBNull.Value;

            try
            {
                switch (dataType.ToLower())
                {
                    case "bool":
                        return bool.Parse(value);
                    case "int":
                    case "int32":
                        return int.Parse(value, CultureInfo.InvariantCulture);
                    case "int64":
                    case "long":
                        return long.Parse(value, CultureInfo.InvariantCulture);
                    case "double":
                        return double.Parse(value, CultureInfo.InvariantCulture);
                    case "datetime":
                        return value;
                    case "datetime2":
                        return ParseDateTime(value);
                    default:
                        return value;
                }
            }
            catch (Exception)
            {
                return value;
            }

        }

        private string GetDefaultSize(string dataType)
        {
            switch (dataType)
            {
                case "bool":
                case "int32":
                case "int64":
                case "dateTime":
                    return "";
                case "double":
                    return "18,0";
                default:
                    return "50";
            }
        }

        public DateTime? ParseDateTime(string value)
        {
            List<string> formats = DateTime.Now.GetDateTimeFormats().ToList();
            formats.Add("dd-MM-yyyy");
            formats.Add("dd/MM/yyyy");
            formats.Add("HH:mm");
            formats.Add("hh:mm");

            if (!DateTime.TryParseExact(value, formats.ToArray(), CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime formattedDateTime))
            {
                return null;
            }

            return formattedDateTime;
        }
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