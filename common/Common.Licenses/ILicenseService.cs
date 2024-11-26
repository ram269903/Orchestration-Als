using Common.Config;
using Common.Licenses.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Common.Licenses
{
    public interface ILicenseService
    {
        void UpdateLicense(string application, AppSettings appsetting);
        Task<LicenseResponse> GetLicense(string application);
    }
}
