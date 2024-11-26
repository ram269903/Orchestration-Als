namespace Common.Config
{
    public class DatabaseSettings {
        public DbConfig BOP { get; set; }
        public DbConfig? Archive { get; set; }
    }
    public class DbConfig
    {
        public string DataProvider { get; set; }
        public string ConnectionString { get; set; }
        public string Database { get; set; }
    }
}
