namespace Common.DataAccess
{
    public interface IDbSettings
    {
        string DataProvider { get; set; }
        string ConnectionString { get; set; }
        string Database { get; set; }
    }
}
