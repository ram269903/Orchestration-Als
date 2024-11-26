using Common.Config;
using Common.DataAccess.MsSql;
using Common.DataAccess.RDBMS;
using Common.Licenses.Models;
using Common.Security;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;


namespace Common.Licenses.MsSql
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

            const string sql = @"SELECT * FROM [dbo].[Licenses] WHERE [Application] = @application";

            var parameters = new List<IDataParameter>
            {
                QueryHelper.CreateSqlParameter("@application", application, SqlDbType.NVarChar)
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

            var license = SecurityHelper.DecryptWithEmbedKey(encLicense.Key,25).Split(',');

            if (license != null)
            {
                licenseResp = new LicenseResponse
                {
                    LicensedTo = license[0],
                    ValidFrom = DateTime.ParseExact(license[1], "dd-MM-yyyy HH:mm:ss", CultureInfo.InvariantCulture),
                    ValidTo = DateTime.ParseExact(license[2], "dd-MM-yyyy HH:mm:ss", CultureInfo.InvariantCulture),
                    //FreeText = license[3],
                    FreeText = "Access Permissions;Data Setup;Reports;Advisories;Archive Viewer;Configuration",
                    //ActiveUsers = Convert.ToInt64(license[4])
                    ActiveUsers = 5
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
            const string sql = "DELETE FROM [dbo].[Licenses] WHERE [Id] = @licenseId";

            var parameters = new List<IDataParameter>
            {
                QueryHelper.CreateSqlParameter("@licenseId", Guid.Parse(licenseId), SqlDbType.UniqueIdentifier)
            };

            return await _queryHelper.ExecuteNonQuery(sql, parameters) == 1;

        }

        private async Task<License> InsertLicense(License license)
        {
            const string sql = @"INSERT [dbo].[Licenses] (
                                [Application],                                
                                [Key],
                                [CreatedBy],
                                [CreatedDate],
                                [UpdatedBy],
                                [UpdatedDate],
                                [IsDeleted])
                            OUTPUT Inserted.ID
                            VALUES (
                                @application,
                                @key,
                                @createdBy,
                                @createdDate,
                                @updatedBy,
                                @updatedDate,
                                @isDeleted)";

            var id = await _queryHelper.ExecuteScalar(sql, Take(license));

            license.Id = id.ToString();

            return license;
        }

        private async Task<License> UpdateLicense(License license)
        {
            const string sql = @"UPDATE [dbo].[Licenses]
                                SET 
                                    [Application] = @application,
                                    [Key] = @key,
                                    [CreatedBy] = @createdBy,
                                    [CreatedDate] = @createdDate,
                                    [UpdatedBy] = @updatedBy,
                                    [UpdatedDate] = @updatedDate,
                                    [IsDeleted] = @isDeleted
                               WHERE 
                                    [Application] = @application";

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
                QueryHelper.CreateSqlParameter("@licenseId", new Guid(license.Id), SqlDbType.UniqueIdentifier),
                QueryHelper.CreateSqlParameter("@application", license.Application, SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@key", license.Key, SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@isDeleted", license.IsDeleted == true? 1: 0, SqlDbType.Bit),
                QueryHelper.CreateSqlParameter("@createdBy", license.CreatedBy, SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@createdDate", license.CreatedDate, SqlDbType.DateTime2),
                QueryHelper.CreateSqlParameter("@updatedBy", license.UpdatedBy, SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@updatedDate", license.UpdatedDate, SqlDbType.DateTime2)
            };

            return parameters;
        }
    }
}
