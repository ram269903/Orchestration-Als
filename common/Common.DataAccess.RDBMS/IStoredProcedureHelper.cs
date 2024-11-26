using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace Common.DataAccess.RDBMS
{
    public interface IStoredProcedureHelper
    {
        Task<DataTable> ExecuteQuery (string storedProcedureName, List<IDataParameter> sqlParameters = null);
        Task<IEnumerable<T>> Read<T>(string storedProcedureName, List<IDataParameter> sqlParameters, Func<IDataReader, T> make);
        Task<object> ExecuteScalar(string storedProcedureName, List<IDataParameter> sqlParameters = null);
        Task<int> ExecuteNonQuery(string storedProcedureName, List<IDataParameter> sqlParameters = null);
    }
}
