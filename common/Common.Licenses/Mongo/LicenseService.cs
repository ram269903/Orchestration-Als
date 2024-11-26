using Common.Config;
using Common.DataAccess;
using Common.DataAccess.Mongo;
using Common.Licenses.Models;
using Common.Security;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Common.Licenses.Mongo
{
    public class LicenseService : ILicenseService
    {
        private readonly IRepository<License> _licensesRepository;
        private const string LicensesRepository = "Licenses";

        public LicenseService(IOptions<DbConfig> dbConfig)
        {
            var databaseConfig = dbConfig.Value;

            var dbSettings = new DbSettings { ConnectionString = databaseConfig.ConnectionString, Database = databaseConfig.Database };

            _licensesRepository = new MongoRepository<License>(dbSettings, LicensesRepository);
        }

        public LicenseService(DbConfig dbConfig)
        {
            var dbSettings = new DbSettings { ConnectionString = dbConfig.ConnectionString, Database = dbConfig.Database };

            _licensesRepository = new MongoRepository<License>(dbSettings, LicensesRepository);
        }

        //public async Task<License> GetLicense(string licenseId)
        //{
        //    if (string.IsNullOrEmpty(licenseId)) return null;

        //    return await _licensesRepository.GetByIdAsync(licenseId);
        //}

        //public async Task<IEnumerable<License>> GetLicenses()
        //{
        //    var licenses = (await _licensesRepository.GetAllAsync(null, null,null, SortOrder.NoSort)).ToList();

        //    return licenses;
        //}

        //public async Task<License> SaveLicense(License license)
        //{
        //    return await _licensesRepository.UpdateOneAsync(license);
        //}

        //public async Task<bool> DeleteLicense(string licenseId)
        //{
        //    return (await _licensesRepository.DeleteByIdAsync(licenseId)) == 1;
        //}

        public async void UpdateLicense(string application, AppSettings appsetting)
        {
            var licFilePath = appsetting.LicenseKeyLocation;
            var licenseKey = string.Empty;

            if (File.Exists(licFilePath))
            {
                licenseKey = File.ReadAllText(licFilePath);

                var license = await _licensesRepository.GetOneAsync(x => x.Application == application);

                if (license == null)
                    license = new License();

                license.Key = licenseKey;
                license.Application = application;

                await _licensesRepository.UpdateOneAsync(license);
            }
        }

        public async Task<LicenseResponse> GetLicense(string application)
        {
            if (string.IsNullOrEmpty(application)) return null;

            var encLicense = await _licensesRepository.GetOneAsync(x => x.Application == application);

            LicenseResponse licenseResp = null;

            var license = SecurityHelper.DecryptWithEmbedKey(encLicense.Key, 25).Split(',');

            if (license != null)
            {
                licenseResp = new LicenseResponse
                {
                    LicensedTo = license[0],
                    ValidFrom = DateTime.ParseExact(license[1], "dd-MM-yyyy HH:mm:ss", CultureInfo.InvariantCulture),
                    ValidTo = DateTime.ParseExact(license[2], "dd-MM-yyyy HH:mm:ss", CultureInfo.InvariantCulture),
                    FreeText = license[3],
                    ActiveUsers = Convert.ToInt64(license[4])
                };
            }

            return licenseResp;
        }
    }
}
