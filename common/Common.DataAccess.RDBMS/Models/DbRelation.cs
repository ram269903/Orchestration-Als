
namespace Common.DataAccess.RDBMS.Model
{
    public class DbRelation
    {
        public string ForeignTable { get; set; }
        public string ForeignKey { get; set; }
        public string ReferencedTable { get; set; }
        public string ReferencedKey { get; set; }

    }
}
