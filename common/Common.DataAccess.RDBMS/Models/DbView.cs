using System.Collections.Generic;

namespace Common.DataAccess.RDBMS.Model
{
    public class DbView : DbSchema
    {
        public string ViewName { get; set; }
        public List<DbColumn> Columns { get; set; }
    }
}
