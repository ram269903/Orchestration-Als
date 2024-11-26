using Common.DataAccess.RDBMS.Model;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace Common.DataAccess.RDBMS
{
    public interface IQueryHelper
    {
        bool IsServerConnected(string connectionString = null);
        Task<DataTable> ExecuteQuery(string sql, List<IDataParameter> sqlParameters = null);
        Task<IEnumerable<T>> Read<T>(string sql, List<IDataParameter> sqlParameters, Func<IDataReader, T> make);
        Task<IDataReader> Read<T>(string sql, List<IDataParameter> sqlParameters);
        Task<object> ExecuteScalar(string sql, List<IDataParameter> sqlParameters = null);
        Task<int> ExecuteNonQuery(string sql, List<IDataParameter> sqlParameters = null);
        bool LoadDataTable(DataTable dataTable, string tableName);
        bool LoadCsvData(string csvFilePath, string tableName, List<DbColumn> columns, string separator = ",");
        DataTable CsvToDataTable(string csvFilePath, string tableName, List<DbColumn> columns, string separator = ",", int? maxRows = null);
        DataTable ExcelToDataTable(string filePath, string tableName, List<DbColumn> columns, int? maxRows = null);
        List<DbColumn> GetTableDefinition(string csvFilePath, string separator = ",");
    }
}
