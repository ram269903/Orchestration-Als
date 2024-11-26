using Common.DataAccess.RDBMS.Models;

namespace Common.DataAccess.RDBMS
{
    public interface IQueryBuilderHelper
    {
        string BuildQuery(Query query);
    }
}
