using Common.DataAccess.RDBMS.Model;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace Common.DataAccess.RDBMS
{
    public interface ITablesHelper
    {
        List<string> GetTableNames();
        Task<IEnumerable<DbTable>> GetTablesDetails(List<string> tablesList = null);
        Task CreateTable(string tableName, List<DbColumn> columnDetails, string schema = null, bool dropCurrentTable = true);
        Task<bool> CheckTableExists(string tableName, string schema = null);
        Task DropTable(string tableName, string schema = null);
        Task TruncateTable(string tableName, string schema = null);
    }
}
