namespace Common.DataAccess.Mongo
{
    public class DbSettings: IDbSettings
    {
        public string ConnectionString { get; set; }
        public string Database { get; set; }
        public string DataProvider { get; set; }
    }
}
