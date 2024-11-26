using System.Collections.Generic;

namespace Common.DataAccess.RDBMS.Model
{
    public class DbTable : DbSchema
    {
        public string TableName { get; set; }
        public IEnumerable<DbColumn> Columns { get; set; }
        public IEnumerable<DbRelation> Relations { get; set; }
    }
}
