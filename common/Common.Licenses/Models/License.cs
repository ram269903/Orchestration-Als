using Common.DataAccess;

namespace Common.Licenses.Models
{
    public class License : DbBaseEntity
    {
        public string Application { get; set; }
        public string Key { get; set; }
    }
}
