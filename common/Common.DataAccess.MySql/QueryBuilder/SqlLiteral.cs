
namespace Common.DataAccess.MsSql.QueryBuilder
{
    public class SqlLiteral
    {
        public string Value { get; set; }
        
        public SqlLiteral(string value)
        {
            Value = value;
        }
    }
}
