using Common.ArchiveOrchestration.Repository;
using Common.MicroBatch.Repository;
using Common.MicroBatch.Repository.Model;
using Common.Orchestration.Repository.Model;
using DSS.Als.Orchestration.Models;
using DSS.Als.Orchestration.CommonProcess;
using DSS.Orchestration.CommonProcess;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Quartz;
using Serilog;
using System.Data;
using System.Net;
using System.Net.Sockets;
using System.Xml.Linq;

namespace RHB.Als.Orchestration.Jobs
{
    [DisallowConcurrentExecution]
    public class ArchivingSchedular : IJob
    {
        private readonly IOrchestrationService _orchestrationService;
        private readonly IArchiveOrchestrationService _archiveOrchestrationService;

        private readonly StatementGeneration _statementGeneration;
        private readonly EtlPaths _etlPaths;
        private readonly Dictionary<string, string> shortnames = new Dictionary<string, string>();
        private readonly HAConfig _haConfig;
        private string processServer = "";
        private readonly MailSettings _mailSettings;
        public ArchivingSchedular(IOptions<StatementGeneration> statmentGeneration, IOrchestrationService orchestrationService, IArchiveOrchestrationService archiveOrchestrationService, IOptions<HAConfig> haConfig, IOptions<EtlPaths> etlPaths, IOptions<MailSettings> mailSettings)
        {
            _orchestrationService = orchestrationService;
            _archiveOrchestrationService = archiveOrchestrationService;
            _statementGeneration = statmentGeneration.Value;
            _etlPaths = etlPaths.Value;
            _haConfig = haConfig.Value;
            _mailSettings = mailSettings.Value;

            string[] lines = Array.Empty<string>();

            try
            {
                lines = File.ReadAllLines(_etlPaths.ShortLongNamePath);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error while loading short name csv file");
            }

            foreach (var l in lines)
            {
                var lsplit = l.Split(',');

                if (lsplit.Length > 1)
                {
                    var newkey = lsplit[0];
                    var newval = lsplit[1]; //+ lsplit[1];
                    shortnames[newkey] = newval;
                }
            }
        }

        public async Task Execute(IJobExecutionContext context)
        {
            try
            {

                string[] stmtList = Directory.GetDirectories(_statementGeneration.AlsInfo.InputFilePath.RootPath.Replace("{type}", "ASB-NM"));

                foreach (var item in stmtList)
                {
                    string[] readyFile = Directory.GetFiles(item, "archive.ready");

                    if (readyFile.Length > 0)
                    {
                        Log.Information("Ready file found in :"+item+ "/archive.ready");

                        MoveFilesToArchive(item);
                    }
                }

                stmtList = Directory.GetDirectories(_statementGeneration.AlsInfo.InputFilePath.RootPath.Replace("{type}", "TL-NM"));

                foreach (var item in stmtList)
                {
                    string[] readyFile = Directory.GetFiles(item, "archive.ready");

                    if (readyFile.Length > 0)
                    {
                        Log.Information("Ready file found in :" + item + "/archive.ready");

                        MoveFilesToArchive(item);
                    }
                }

                stmtList = Directory.GetDirectories(_statementGeneration.AlsInfo.InputFilePath.RootPath.Replace("{type}", "PF-NM"));

                foreach (var item in stmtList)
                {
                    string[] readyFile = Directory.GetFiles(item, "archive.ready");

                    if (readyFile.Length > 0)
                    {
                        Log.Information("Ready file found in :" + item + "/archive.ready");

                        MoveFilesToArchive(item);
                    }
                }

                stmtList = Directory.GetDirectories(_statementGeneration.AlsInfo.InputFilePath.RootPath.Replace("{type}", "MG-NM"));

                foreach (var item in stmtList)
                {
                    string[] readyFile = Directory.GetFiles(item, "archive.ready");

                    if (readyFile.Length > 0)
                    {
                        Log.Information("Ready file found in :" + item + "/archive.ready");

                        MoveFilesToArchive(item);
                    }
                }

            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error in Archive Scheduler job. ");
            }

            await Task.CompletedTask;
        }
        public async void MoveFilesToArchive(string productPath)
        {

            try
            {
                string sourcePath = productPath.Replace("INPUT", "OUTPUT");

               

                string destinationPath = GetArchiveServerInfo(sourcePath);

                if (destinationPath == "null" || string.IsNullOrEmpty(destinationPath))
                {
                    Log.Information("Server not found to process batch files.");
                    return;
                }

                Log.Information("UT Archive iniated for:" + productPath);

                File.Move(productPath + "/archive.ready", productPath + "/archive.inprogress", true);

                if (File.Exists(productPath + "/archive.ready"))
                    File.Delete(productPath + "/archive.ready");

                processServer = processServer.Trim() == "Server1" ? "sa01" : processServer.Trim() == "Server2" ? "sa02" : processServer.Trim() == "Server3" ? "sa03" : processServer;

                string stmtType = new DirectoryInfo(productPath).Name;

                string productFullName = Helper.GetFullNames(stmtType);

                Log.Information("Destination path: " + destinationPath);

                Log.Information("Destination Folder for Vault:" + destinationPath);

                string cycleIdFile = productPath.ToLower().Contains("asb") ? Path.Combine(_etlPaths.CycleIdPath.Replace("{type}", "ASB-NM"), "asb_1_cycle.id") : productPath.ToLower().Contains("tl") ? Path.Combine(_etlPaths.CycleIdPath.Replace("{type}", "TL-NM"), "tl_1_cycle.id") : productPath.ToLower().Contains("mg") ? Path.Combine(_etlPaths.CycleIdPath.Replace("{type}", "MG-NM"), "mg_1_cycle.id") : Path.Combine(_etlPaths.CycleIdPath.Replace("{type}", "PF-NM"), "pf_1_cycle.id");

                var cycleId = File.ReadLines(cycleIdFile).ElementAt(0);

                _ = _orchestrationService.UpdateBatchHistoryTable("Archive", productFullName, processServer+":in-progress", cycleId);

                bool status = await BackupandMove(sourcePath, destinationPath);

                Log.Information($"Final status for {sourcePath} is : " + status);

                if (!status)
                {
                    try
                    {
                        string mailbody = File.ReadAllText(_mailSettings.BodyFilePath);

                        File.WriteAllText(Path.Combine(Path.GetDirectoryName(_mailSettings.BodyFilePath), "Body.txt"), mailbody.Replace("{ERROR}", "Error in Archive server please check in logs").Replace("{datetime}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));

                        _ = await MailProcess.UrlHit($"http://{_mailSettings.ApiUrl.Replace("{subjectPath}", _mailSettings.SubjectFilePath).Replace("{bodyPath}", Path.Combine(Path.GetDirectoryName(_mailSettings.BodyFilePath), "Body.txt"))}");
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Mail sending error");
                    }
                    File.WriteAllText(productPath + "/archive.error", "1");

                    var data = _orchestrationService.UpdateProgressTable("Fail", cycleId).Result;

                    _ = _orchestrationService.UpdateBatchHistoryTable("Archive", productFullName, processServer+":failed", cycleId);

                    //_ = await MailProcess.UrlHit("");
                }
                else
                {
                    File.Move(productPath + "/archive.inprogress", productPath + "/archive.completed");
                    _ = _orchestrationService.UpdateBatchHistoryTable("Archive", productFullName, processServer+":completed", cycleId);
                }

                Log.Information("Archiving files moving and backup process has been completed.");
            }
            catch (Exception ex)
            {
                string mailbody = File.ReadAllText(_mailSettings.BodyFilePath);

                File.WriteAllText(Path.Combine(Path.GetDirectoryName(_mailSettings.BodyFilePath), "Body.txt"), mailbody.Replace("{ERROR}", "Error in Archive server please check in logs").Replace("{datetime}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));

                Log.Information($"http://{_mailSettings.ApiUrl.Replace("{subjectPath}", _mailSettings.SubjectFilePath).Replace("{bodyPath}", Path.Combine(Path.GetDirectoryName(_mailSettings.BodyFilePath), "SABody.txt"))}");

                _ = await MailProcess.UrlHit($"http://{_mailSettings.ApiUrl.Replace("{subjectPath}", _mailSettings.SubjectFilePath).Replace("{bodyPath}", Path.Combine(Path.GetDirectoryName(_mailSettings.BodyFilePath), "SABody.txt"))}");

                string cycleIdFile = productPath.ToLower().Contains("asb") ? Path.Combine(_etlPaths.CycleIdPath.Replace("{type}", "ASB-NM"), "asb_1_cycle.id") : productPath.ToLower().Contains("tl") ? Path.Combine(_etlPaths.CycleIdPath.Replace("{type}", "TL-NM"), "tl_1_cycle.id") : productPath.ToLower().Contains("mg") ? Path.Combine(_etlPaths.CycleIdPath.Replace("{type}", "MG-NM"), "mg_1_cycle.id") : Path.Combine(_etlPaths.CycleIdPath.Replace("{type}", "PF-NM"), "pf_1_cycle.id");

                var cycleId = File.ReadLines(cycleIdFile).ElementAt(0);

                var data = _orchestrationService.UpdateProgressTable("Fail", cycleId).Result;

                _ = _orchestrationService.UpdateBatchHistoryTable("Archive", "", "Fail", cycleId);

                File.WriteAllText(productPath + "/archive.error", "1");

                Log.Error(ex, "Error while calling backup and move process..");
            }
        }


        public static bool CopyFile(string inputFilePath, string outputFilePath)
        {
            try
            {
                using (Stream inStream = File.Open(inputFilePath, FileMode.Open))
                {
                    using (Stream outStream = File.Create(outputFilePath))
                    {
                        while (inStream.Position < inStream.Length)
                        {
                            outStream.WriteByte((byte)inStream.ReadByte());
                        }
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error while copying");
                return false;
            }

        }

        public async Task<bool> BackupandMove(string statmentPath, string destinationPath)
        {

            try
            {
                Log.Information($"Files moving process started for download folder. source: {statmentPath}, Destination : {destinationPath}");

                string[] inputFolders = Directory.GetDirectories(Path.Combine(statmentPath, "ARS"));

                string cycleIdFile = statmentPath.ToLower().Contains("asb") ? Path.Combine(_etlPaths.CycleIdPath.Replace("{type}", "ASB-NM"), "asb_1_cycle.id") : statmentPath.ToLower().Contains("tl") ? Path.Combine(_etlPaths.CycleIdPath.Replace("{type}", "TL-NM"), "tl_1_cycle.id") : statmentPath.ToLower().Contains("mg") ? Path.Combine(_etlPaths.CycleIdPath.Replace("{type}", "MG-NM"), "mg_1_cycle.id") : Path.Combine(_etlPaths.CycleIdPath.Replace("{type}", "PF-NM"), "pf_1_cycle.id");
                var cycleId = File.ReadLines(cycleIdFile).ElementAt(0);
                string stmtdateFile = cycleIdFile.Replace("cycle.id", "stmt.date");

                string stmtdate = File.ReadLines(stmtdateFile).ElementAt(0);

                string backUpFolder = Path.Combine(statmentPath, "ARS", "BACKUP", cycleId);

                Directory.CreateDirectory(backUpFolder);

                string dbdumpdtatus = "";

                foreach (var item in inputFolders)
                {
                    if (item.Contains("BACKUP"))
                        continue;

                    var files = Directory.GetFiles(item);

                    string batchFolder = Path.Combine(backUpFolder, new DirectoryInfo(item).Name);

                    Log.Information("Backup folder path: " + batchFolder);

                    string destinationfile = "";

                    if (dbdumpdtatus == "Error")
                        break;

                    Directory.CreateDirectory(batchFolder);

                    foreach (var file in files)
                    {
                        string backup = Path.Combine(batchFolder, Path.GetFileName(file));

                        Log.Information("ARS back file Path: " + backup);

                        if (file.Contains(".jrn"))
                        {

                            dbdumpdtatus = SaveJrnFileToDatabase(file, stmtdate).Result;

                            Log.Information($"Database dump status for {file} is {dbdumpdtatus}");

                            if (dbdumpdtatus == "Error")
                                break;

                            Log.Information($"File copied from {file} to {backup}.");

                            bool copystatus = CopyFile(file, backup);

                            if (dbdumpdtatus !="Error")
                            {

                                destinationfile = Path.Combine(destinationPath, Path.GetFileName(file).Replace("ln_", "LNSTMT_") + ".done");

                                Log.Information($"File moving from {file} to destination folder {destinationfile}");


                                File.Move(file, destinationfile, true);
                            }
                        }
                        else if (dbdumpdtatus !="Error")
                        {

                            Log.Information($"File copied from {file} to {backup}.");

                            bool copystatus = CopyFile(file, backup);


                            destinationfile = Path.Combine(destinationPath, Path.GetFileName(file).Replace("ln_", "LNSTMT_") + ".done");

                            Log.Information($"File moving from {file} to destination folder {destinationfile}");

                            File.Move(file, destinationfile, true);

                        }
                    }

                    if (Directory.GetFiles(item).Length == 0)
                        Directory.Delete(item);
                }

                inputFolders = Directory.GetDirectories(Path.Combine(statmentPath, "ARS"));

                foreach (var item in inputFolders)
                {
                    if (item.Contains("BACKUP"))
                        continue;

                    var files = Directory.GetFiles(item);

                    string batchFolder = Path.Combine(backUpFolder, new DirectoryInfo(item).Name);

                    Log.Information("Backup folder path: " + batchFolder);

                    Directory.CreateDirectory(batchFolder);

                    foreach (var file in files)
                    {
                        string backup = Path.Combine(batchFolder, Path.GetFileName(file));

                        File.Move(file, backup);
                    }

                    Directory.Delete(item);
                }
                Log.Information("Zip process initiated for " + backUpFolder);

                if (Directory.GetDirectories(backUpFolder).Count() == 0)
                {
                    Log.Information("Empty folder: "+backUpFolder);
                    Directory.Delete(backUpFolder);
                    if (dbdumpdtatus=="Error")
                        return false;
                    return true;
                }

                string backupscript = Path.Combine(_etlPaths.ETLHomePath, "TEMP", "gzip" + DateTime.Now.ToString("ddMMyyyyHHmmssfff")+".sh");

                try
                {
                    File.WriteAllText(backupscript, $"cd {Path.Combine(statmentPath, "ARS", "BACKUP")}\ntar -zcvf {cycleId}.tar.gz {cycleId}\nrm -r {cycleId}");

                    string process = Helper.ProcessCommandLine(backupscript);

                    Log.Information("GZip process status:" + process);

                    File.Delete(backupscript);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error while making gzip script");
                    return false;
                }
                if (dbdumpdtatus=="Error")
                    return false;

                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error while moving files to download folder: " + statmentPath);

                return false;
            }
        }

       
        public async Task<string> SaveJrnFileToDatabase(string filename, string statementDate)
        {
            try
            {
                Log.Information("Meta data insert initiated for "+filename);

                string fileDirectoryPath = Path.GetDirectoryName(filename);

                string stmtType = fileDirectoryPath.Split("/")[6];

                Log.Information($"Statment type {stmtType}");

                string stmtTypeFullName = Helper.GetFullNames(stmtType);

                Log.Information("Statement full name: "+stmtTypeFullName);

                byte[] file = File.ReadAllBytes(filename);

                Stream stream = new MemoryStream(file);

                XDocument xml = XDocument.Load(stream);

                IEnumerable<XElement> xDoc = XDocument.Parse(xml.ToString()).Descendants("document");

                string genDate = XDocument.Parse(xml.ToString()).Descendants("jobdata").Descendants("datetime").FirstOrDefault().Value;

                var xmldata = (from item in xDoc

                               let cycleid = (item.Elements("DDSDocValue")
                                .Where(i => (string)i.Attribute("name") == "Cycle_ID").Select(i => i.Value)).FirstOrDefault()
                               let attachname1 = (item.Elements("DDSDocValue")
                                    .Where(i => (string)i.Attribute("name") == "AttachName1").Select(i => i.Value)).FirstOrDefault()
                               let subject = (item.Elements("DDSDocValue")
                                    .Where(i => (string)i.Attribute("name") == "Subject").Select(i => i.Value)).FirstOrDefault()
                               let email = (item.Elements("DDSDocValue")
                                   .Where(i => (string)i.Attribute("name") == "Email").Select(i => i.Value)).FirstOrDefault()
                               let name1 = (item.Elements("DDSDocValue")
                                   .Where(i => (string)i.Attribute("name") == "Name_1").Select(i => i.Value)).FirstOrDefault()
                               let addr1 = (item.Elements("DDSDocValue")
                                   .Where(i => (string)i.Attribute("name") == "Addr_1").Select(i => i.Value)).FirstOrDefault()
                               let addr2 = (item.Elements("DDSDocValue")
                                   .Where(i => (string)i.Attribute("name") == "Addr_2").Select(i => i.Value)).FirstOrDefault()
                               let postcode = (item.Elements("DDSDocValue")
                                   .Where(i => (string)i.Attribute("name") == "Postcode").Select(i => i.Value)).FirstOrDefault()
                               let state = (item.Elements("DDSDocValue")
                                   .Where(i => (string)i.Attribute("name") == "State").Select(i => i.Value)).FirstOrDefault()
                               let cisnumber = (item.Elements("DDSDocValue")
                                   .Where(i => (string)i.Attribute("name") == "CIS_Number").Select(i => i.Value)).FirstOrDefault()
                               let accountnumber = (item.Elements("DDSDocValue")
                                   .Where(i => (string)i.Attribute("name") == "Account_Number").Select(i => i.Value)).FirstOrDefault()
                               let deliverymethod = (item.Elements("DDSDocValue")
                                    .Where(i => (string)i.Attribute("name") == "Delivery_Method").Select(i => i.Value)).FirstOrDefault()
                               let statementType = (item.Elements("DDSDocValue")
                               .Where(i => (string)i.Attribute("name") == "Statement_Type").Select(i => i.Value)).FirstOrDefault()
                               let dob = (item.Elements("DDSDocValue")
                                   .Where(i => (string)i.Attribute("name") == "DOB").Select(i => i.Value)).FirstOrDefault()
                               let idnumber = (item.Elements("DDSDocValue")
                                   .Where(i => (string)i.Attribute("name") == "Id_Number").Select(i => i.Value)).FirstOrDefault()
                               let productname = (item.Elements("DDSDocValue")
                                    .Where(i => (string)i.Attribute("name") == "Product_Name").Select(i => i.Value)).FirstOrDefault()
                               let productcode = (item.Elements("DDSDocValue")
                                    .Where(i => (string)i.Attribute("name") == "Product_Code").Select(i => i.Value)).FirstOrDefault()
                               let productdescription = (item.Elements("DDSDocValue")
                               .Where(i => (string)i.Attribute("name") == "Loan_Type").Select(i => i.Value)).FirstOrDefault()
                               let bankcode = (item.Elements("DDSDocValue")
                                    .Where(i => (string)i.Attribute("name") == "Bank_Code").Select(i => i.Value)).FirstOrDefault()
                               let documenttype = (item.Elements("DDSDocValue")
                                   .Where(i => (string)i.Attribute("name") == "Document_Type").Select(i => i.Value)).FirstOrDefault()
                               let staff = (item.Elements("DDSDocValue")
                                   .Where(i => (string)i.Attribute("name") == "Staff").Select(i => i.Value)).FirstOrDefault()
                               let division = (item.Elements("DDSDocValue")
                                   .Where(i => (string)i.Attribute("name") == "Division").Select(i => i.Value)).FirstOrDefault()
                               let premier = (item.Elements("DDSDocValue")
                                  .Where(i => (string)i.Attribute("name") == "Premier").Select(i => i.Value)).FirstOrDefault()
                               let entity = (item.Elements("DDSDocValue")
                                  .Where(i => (string)i.Attribute("name") == "Entity").Select(i => i.Value)).FirstOrDefault()
                               let fileName = (item.Elements("DDSDocValue")
                               .Where(i => (string)i.Attribute("name") == "File_Name").Select(i => i.Value)).FirstOrDefault()
                               let divisionCode = (item.Elements("DDSDocValue")
                                  .Where(i => (string)i.Attribute("name") == "Division_Code").Select(i => i.Value)).FirstOrDefault()
                               let noofpages = (item.Elements("DDSDocValue")
                                .Where(i => (string)i.Attribute("name") == "No_Of_Pages").Select(i => i.Value)).FirstOrDefault()
                               let foreignaddressindicator = (item.Elements("DDSDocValue")
                                .Where(i => (string)i.Attribute("name") == "Foreign_Address_Indicator").Select(i => i.Value)).FirstOrDefault()
                               let badindicator = (item.Elements("DDSDocValue")
                               .Where(i => (string)i.Attribute("name") == "Bad_Indicator").Select(i => i.Value)).FirstOrDefault()

                               let accNo = (item.Descendants("AccNo").FirstOrDefault().Value)
                               let vendorId = (item.Descendants("VendorId").FirstOrDefault().Value)
                               let docTypeId = (item.Descendants("DocTypeId").FirstOrDefault().Value)
                               let docID = (item.Attribute("docID").Value)


                               select new GenerateLog
                               {
                                   Id = accNo,
                                   Name_1 = name1,
                                   AccountNumber = accountnumber,
                                   ProductName = productname,
                                   ProductType = stmtTypeFullName,
                                   StatementDate = DateTime.ParseExact(statementDate, "yyyyMMdd", null),
                                   DocumentType = documenttype,
                                   CycleID = cycleid,
                                   Address1 = addr1,
                                   Address2 = addr2,
                                   PostCode = postcode,
                                   State = state,
                                   BankCode = bankcode,
                                   CISNumber = cisnumber,
                                   DOB = dob,
                                   IdNumber = idnumber,
                                   DivisionCode = divisionCode,
                                   FileName = fileName,
                                   Division = division,
                                   Premier = premier,
                                   Entity = entity,
                                   Staff = staff,
                                   Email = email,
                                   NoOfPages = noofpages,
                                   DeliveryMethod = deliverymethod,
                                   PrintingVendorIndicator = foreignaddressindicator,
                                   Status = "SUCCESS",
                                   IsDeleted = false,
                                   IsDisabled = badindicator?.ToLower() == "y" ? true : false,
                                   VendorId = vendorId,
                                   DocTypeId = docTypeId,
                                   DocId = docID,
                                   ProductCode = productcode,
                                   ProductDescription = productdescription,
                                   BadIndicator = badindicator,

                                   LStaff = shortnames.GetValueOrDefault(staff),
                                   LDivision = shortnames.GetValueOrDefault(division),
                                   LPremier = shortnames.GetValueOrDefault(premier),
                                   LDeliveryMethod = shortnames.GetValueOrDefault(deliverymethod),
                                   LEntity = shortnames.GetValueOrDefault(entity),
                                   LBankCode = shortnames.GetValueOrDefault(bankcode),
                                   LProductName = shortnames.GetValueOrDefault(productname),
                                   CreatedDate = DateTime.Now,
                                   UpdatedDate = DateTime.Now,
                                   BatchType = "NM"
                               }).ToList();

                int internalFileCount = 1;

                var suppressedAccounts = await _orchestrationService.GetArchiveSuppressAccountNumbers(statementDate);

                int supretry = 0;

                while (suppressedAccounts == null && supretry < _statementGeneration.DbRetryAttempts)
                {
                    Thread.Sleep(_statementGeneration.DbRetryWaitTime*1000);

                    Log.Information($"Get Suppresion records Retrying for db connection, attempt no {supretry} ");

                    suppressedAccounts = await _orchestrationService.GetArchiveSuppressAccountNumbers(statementDate);

                    supretry++;
                }

                if (suppressedAccounts == null)
                    return "Error";

                suppressedAccounts.RemoveAll(item => item == null);

                Log.Information("Suppressed accounts in this month for all stmt's is: "+suppressedAccounts.Count);

                foreach (var item in xmldata)
                {

                    try
                    {
                        item.FileName = stmtType.Replace("-", "_") + "_" + item?.AccountNumber.Substring(10, 4) + "_" + item.StatementDate.ToString("yyyyMMdd", null) + internalFileCount.ToString("D7") + ".pdf";

                        var suppressedAccount = suppressedAccounts?.Where(x => x.Contains(item?.AccountNumber))?.Distinct()?.FirstOrDefault();

                        if (suppressedAccount != null)
                        {
                            Log.Information("Account Suppresed: " + suppressedAccount);

                            var suppressionReport = new SuppressionReport
                            {
                                AccountNumber = item.AccountNumber,
                                CISNumber = item.CISNumber,
                                CycleID = item.CycleID,
                                DeliveryMethod = item.DeliveryMethod,
                                EmailAddress = item.Email,
                                Dob = item.DOB,
                                FileName = item.FileName,
                                ProductName = item.ProductName,
                                StatementDate = item.StatementDate,
                                Description=item.ProductDescription,
                                Status = item.Status,
                                ProductType = item.ProductType,
                                ProductCode= item.ProductCode,
                                Remarks = "Suppressed at Archive"
                            };

                            var result = await _orchestrationService.SaveSuppressionRecord(suppressionReport);

                            int retry = 0;

                            while (result == null && retry < _statementGeneration.DbRetryAttempts)
                            {
                                Thread.Sleep(_statementGeneration.DbRetryWaitTime*1000);

                                Log.Information($"Suppresion Retrying for db connection, attempt no {retry} for record: "+item.Id);

                                result = await _orchestrationService.SaveSuppressionRecord(suppressionReport);

                                retry++;
                            }
                            if(result==null)
                                return "Error";
                        }

                        else
                        {

                            Log.Information("Meta data insertion for: " + item.AccountNumber + "::" + item.ProductName + " Filename: " + item.FileName);

                            var status = await _archiveOrchestrationService.SaveArsRecordMetaData(item, item.ProductName);

                            int retry = 1;

                            while (status == null && retry <= _statementGeneration.DbRetryAttempts)
                            {
                                Thread.Sleep(_statementGeneration.DbRetryWaitTime * 1000);

                                Log.Information($"Metadata Retrying for db connection, attempt no {retry} for record: " + item.Id);

                                status = await _archiveOrchestrationService.SaveArsRecordMetaData(item, item.ProductName);

                                retry++;
                            }
                            if (status == null)
                                return "Error";
                        }

                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Error in database insert");
                        return "Error";
                    }

                    internalFileCount = internalFileCount + 1;

                }

                string jrndata = File.ReadAllText(filename);

                File.WriteAllText(filename, jrndata.Replace($"<datetime>{genDate}</datetime>", $"<datetime>{DateTime.ParseExact(statementDate, "yyyyMMdd", null).ToString("yyyyMMdd")}{"235959"}</datetime>"));

                return "Success";
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error in database insert");

                return "Error";
            }
        }

        public string GetArchiveServerInfo(string inputFilePath)
        {
            try
            {
                if (string.IsNullOrEmpty(_haConfig.ProductSettings[inputFilePath.Split("/")[2]].Servers) || string.IsNullOrWhiteSpace(_haConfig.ProductSettings[inputFilePath.Split("/")[2]].Servers))
                {
                    Log.Information("Please Provide Servers Priority");
                    return "null";
                }
                string currentPort = string.Empty;

                var Addresslist = Dns.GetHostEntry(Dns.GetHostName()).AddressList.Select(x => x.ToString()).ToList();

                //Addresslist.ForEach(Log.Information);

                List<string> unavailableServers = new List<string>();
                var availableServer = "null";
                var priorityServers = _haConfig.ProductSettings[inputFilePath.Split("/")[2]].Servers.Split(',');
                var archiveServers = JsonConvert.SerializeObject(_haConfig.ArchiveServers);
                var serversMapping1 = JsonConvert.DeserializeObject<Server>(archiveServers);

                Dictionary<string, string> serversMapping = serversMapping1.GetType().GetProperties()
    .ToDictionary(x => x.Name, x => x.GetValue(serversMapping1)?.ToString() ?? "");

                foreach (var server in priorityServers)
                {
                    var ipPort = serversMapping[server].ToString().Split(',');
                    try
                    {
                        //1st Port
                        TcpClient tcpClient = new TcpClient();
                        currentPort = ipPort[2];
                        tcpClient.Connect(ipPort[0], Convert.ToInt32(ipPort[2]));
                        tcpClient.Close();

                        //2nd Port
                        TcpClient tcpClient1 = new TcpClient();
                        currentPort = ipPort[1];
                        tcpClient1.Connect(ipPort[0], Convert.ToInt32(ipPort[1]));
                        tcpClient1.Close();

                        if (Addresslist.Contains(ipPort[0]))
                        {
                            Log.Information($@"Archival ports {ipPort[1]},{ipPort[2]} are open in server {ipPort[0]}");
                            availableServer = server;
                            break;
                        }
                        else
                        {
                            Log.Information($@"{server} is up and running according to priority");
                            availableServer = "null";
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        unavailableServers.Add(server);
                        //Log.Information($@"Exception {ex}");
                        Log.Information($@"Port {currentPort} is not reachable in server {ipPort[0]}");
                        continue;
                    }
                }

                var foldrePaths = JsonConvert.SerializeObject(_haConfig.ArchiveDowloadFolderPaths);
                var serverFolderMapping1 = JsonConvert.DeserializeObject<Server>(foldrePaths);

                Dictionary<string, string> serverFolderMapping = serverFolderMapping1.GetType().GetProperties()
       .ToDictionary(x => x.Name, x => x.GetValue(serverFolderMapping1)?.ToString() ?? "");

                foreach (var unavailableServer in unavailableServers)
                {
                    string[] files = Directory.GetFiles(serverFolderMapping[unavailableServer]);
                    if (files.Count() > 0)
                    {
                        foreach (var file in files)
                        {
                            File.Move(file, Path.Combine(serverFolderMapping[availableServer], Path.GetFileName(file)));
                        }
                    }
                }
                processServer = availableServer=="null" ? "null" : availableServer;

                return availableServer=="null" ? "null" : serverFolderMapping[availableServer];
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error while doing HA");
                return "null";
            }
        }
    }
}
