using Common.DataAccess.RDBMS.Model;
using System.Collections.Generic;

namespace Common.DataAccess.RDBMS
{
    public interface IViewsHelper
    {
        List<string> GetViewNames();

        List<DbView> GetViewsDetails(List<string> viewsList = null);

        void CreateView(string viewName, string sqlQuery);

        void DeleteView(string viewName);
    }
}
