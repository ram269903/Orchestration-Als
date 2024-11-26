using Common.Config;
using Common.DataAccess.PostgreSql;
using Common.DataAccess.RDBMS;
using Common.Licenses.Models;
using Common.Security;
using Microsoft.Extensions.Options;
using NpgsqlTypes;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Common.Licenses.PgSql
{
    public class LicenseService : ILicenseService
    {
        private readonly QueryHelper _queryHelper = null;

        public LicenseService(IOptions<DbConfig> dbConfig)
        {
            var databaseConfig = dbConfig.Value;

            _queryHelper = new QueryHelper(databaseConfig.ConnectionString);
        }

        public LicenseService(DbConfig dbConfig)
        {
            _queryHelper = new QueryHelper(dbConfig.ConnectionString);
        }

        public async void UpdateLicense(string application, AppSettings appsetting)
        {
            var licFilePath = appsetting.LicenseKeyLocation;
            
            if (File.Exists(licFilePath))
            {
                string licenseKey = File.ReadAllText(licFilePath);
                var license = await GetLicenseByApplication(application);

                if (license == null)
                    license = new License { Application = application, Key = licenseKey };
                else
                    license.Key = licenseKey;

                await SaveLicense(license);
            }
        }

        private async Task<License> GetLicenseByApplication(string application)
        {
            if (string.IsNullOrEmpty(application)) return null;

            const string sql = @"SELECT * FROM Licenses WHERE Application = @application";

            var parameters = new List<IDataParameter>
            {
                QueryHelper.CreateSqlParameter("@application", application, NpgsqlDbType.Varchar)
            };

            var license = (await _queryHelper.Read(sql, parameters, Make)).FirstOrDefault();

            return license;
        }

        //public async Task<IEnumerable<License>> GetLicenses()
        //{
        //    string sql = @"SELECT * FROM [dbo].[Licenses] WHERE ";

        //    var licenses = (await _queryHelper.Read(sql, null, Make)).ToList();

        //    return licenses;
        //}

        public async Task<LicenseResponse> GetLicense(string application)
        {
            if (string.IsNullOrEmpty(application)) return null;

            var encLicense = await GetLicenseByApplication(application);

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

        public async Task<License> SaveLicense(License license)
        {
            if (string.IsNullOrEmpty(license.Id))
                return await InsertLicense(license);
            else
                return await UpdateLicense(license);
        }

        public async Task<bool> DeleteLicense(string licenseId)
        {
            const string sql = "DELETE FROM Licenses WHERE Id = @licenseId";

            var parameters = new List<IDataParameter>
            {
                QueryHelper.CreateSqlParameter("@licenseId", licenseId, NpgsqlDbType.Varchar)
            };

            return await _queryHelper.ExecuteNonQuery(sql, parameters) == 1;

        }

        private async Task<License> InsertLicense(License license)
        {
            const string sql = @"INSERT INTO Licenses (
                                Application,                                
                                Key,
                                CreatedBy,
                                CreatedDate,
                                UpdatedBy,
                                UpdatedDate,
                                IsDeleted)
                            VALUES (
                                @application,
                                @key,
                                @createdBy,
                                @createdDate,
                                @updatedBy,
                                @updatedDate,
                                @isDeleted) RETURNING Id;";

            var id = await _queryHelper.ExecuteScalar(sql, Take(license));

            license.Id = id.ToString();

            return license;
        }

        private async Task<License> UpdateLicense(License license)
        {
            const string sql = @"UPDATE Licenses
                                SET 
                                    Application = @application,
                                    Key = @key,
                                    CreatedBy = @createdBy,
                                    CreatedDate = @createdDate,
                                    UpdatedBy = @updatedBy,
                                    UpdatedDate = @updatedDate,
                                    IsDeleted = @isDeleted
                               WHERE 
                                    Application = @application";

            _ = await _queryHelper.ExecuteNonQuery(sql, Take(license));

            return license;
        }

        private readonly Func<IDataReader, License> Make = reader =>
            new License
            {
                Id = reader["Id"].AsString(),
                Application = reader["Application"].AsString(),
                Key = reader["Key"].AsString(),
                IsDeleted = reader["IsDeleted"].AsBool(),
                CreatedBy = reader["CreatedBy"].AsString(),
                CreatedDate = reader.GetNullableDateTime("CreatedDate"),
                UpdatedBy = reader["UpdatedBy"].AsString(),
                UpdatedDate = reader.GetNullableDateTime("UpdatedDate")
            };

        private List<IDataParameter> Take(License license)
        {
            if (string.IsNullOrEmpty(license.Id))
                license.Id = Guid.NewGuid().ToString();

            var parameters = new List<IDataParameter>
            {
                QueryHelper.CreateSqlParameter("@licenseId", new Guid(license.Id), NpgsqlDbType.Uuid),
                QueryHelper.CreateSqlParameter("@application", license.Application, NpgsqlDbType.Varchar),
                QueryHelper.CreateSqlParameter("@key", license.Key, NpgsqlDbType.Text),
                QueryHelper.CreateSqlParameter("@isDeleted", license.IsDeleted, NpgsqlDbType.Boolean),
                QueryHelper.CreateSqlParameter("@createdBy", license.CreatedBy, NpgsqlDbType.Varchar),
                QueryHelper.CreateSqlParameter("@createdDate", license.CreatedDate, NpgsqlDbType.Timestamp),
                QueryHelper.CreateSqlParameter("@updatedBy", license.UpdatedBy, NpgsqlDbType.Varchar),
                QueryHelper.CreateSqlParameter("@updatedDate", license.UpdatedDate, NpgsqlDbType.Timestamp)
            };

            return parameters;
        }
    }
}
