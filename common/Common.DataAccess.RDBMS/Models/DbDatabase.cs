
namespace Common.DataAccess.RDBMS.Model
{
    public class DbDatabase : DbServer
    {
        public string DatabaseName { get; set; }
        public string UserId { get; set; }
        public string Password { get; set; }
    }
}
