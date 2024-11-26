using Common.MicroBatch.Repository;
using Common.MicroBatch.Repository.Model;
using Common.Orchestration.Repository.Model;
using DSS.Als.Orchestration.Models;
using DSS.Als.Orchestration.CommonProcess;
using Microsoft.Extensions.Options;
using Quartz;
using Serilog;
using System.Xml.Linq;

namespace RHB.Als.Orchestration.Jobs
{
    public class DeliverySchedular : IJob
    {
        private readonly IOrchestrationService _orchestrationService;
        private readonly DeliveryPaths _deliveryPaths;
        private readonly StatementGeneration _statementGeneration;
        private readonly EtlPaths _etlPaths;
        private readonly AppConfig _config;
        public DeliverySchedular(IOptions<DeliveryPaths> deliveryPaths, IOptions<StatementGeneration> statmentGeneration, IOrchestrationService orchestrationService, IOptions<EtlPaths> etlPaths, IOptions<AppConfig> config)
        {
            _orchestrationService = orchestrationService;
            _deliveryPaths = deliveryPaths.Value;
            _statementGeneration = statmentGeneration.Value;
            _etlPaths = etlPaths.Value;
            _config = config.Value;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            try
            {
                string[] stmtList = Directory.GetDirectories(_statementGeneration.AlsInfo.InputFilePath.RootPath.Replace("{type}","ASB-NM"));

                foreach (var item in stmtList)
                {
                    string[] readyFile = Directory.GetFiles(item, "delivery.ready");

                    if (readyFile.Length > 0)
                    {
                        MoveFilesToDelivery(item);
                        return;
                    }
                }

                stmtList = Directory.GetDirectories(_statementGeneration.AlsInfo.InputFilePath.RootPath.Replace("{type}", "TL-NM"));

                foreach (var item in stmtList)
                {
                    string[] readyFile = Directory.GetFiles(item, "delivery.ready");

                    if (readyFile.Length > 0)
                    {
                        MoveFilesToDelivery(item);
                        return;
                    }
                }

                stmtList = Directory.GetDirectories(_statementGeneration.AlsInfo.InputFilePath.RootPath.Replace("{type}", "PF-NM"));

                foreach (var item in stmtList)
                {
                    string[] readyFile = Directory.GetFiles(item, "delivery.ready");

                    if (readyFile.Length > 0)
                    {
                        MoveFilesToDelivery(item);
                        return;
                    }
                }

                stmtList = Directory.GetDirectories(_statementGeneration.AlsInfo.InputFilePath.RootPath.Replace("{type}", "MG-NM"));

                foreach (var item in stmtList)
                {
                    string[] readyFile = Directory.GetFiles(item, "delivery.ready");

                    if (readyFile.Length > 0)
                    {
                        MoveFilesToDelivery(item);
                        return;
                    }
                }

            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error in Delivery Scheduler job. ");
            }

            await Task.CompletedTask;
        }

        public async void MoveFilesToDelivery(string productPath)
        {
            try
            {
                var deliveryScheduler = _config.JobSettings["DeliverySchedular"];

                Thread.Sleep(deliveryScheduler.ProcessInternalWaitTime * 60000);

                string stmtType = new DirectoryInfo(productPath).Name;

                string productFullName = Helper.GetFullNames(stmtType);

                if (File.Exists(Path.Combine(productPath, "delivery.inprogress")))
                    return;

                Log.Information("Delivery iniated for:" + productPath);

                File.Move(productPath + "/delivery.ready", productPath + "/delivery.inprogress");

                string cycleIdFile = productPath.ToLower().Contains("asb") ? Path.Combine(_etlPaths.CycleIdPath.Replace("{type}", "ASB-NM"), "asb_1_cycle.id") : productPath.ToLower().Contains("tl") ? Path.Combine(_etlPaths.CycleIdPath.Replace("{type}", "TL-NM"), "tl_1_cycle.id") : productPath.ToLower().Contains("mg") ? Path.Combine(_etlPaths.CycleIdPath.Replace("{type}", "MG-NM"), "mg_1_cycle.id") : Path.Combine(_etlPaths.CycleIdPath.Replace("{type}", "PF-NM"), "pf_1_cycle.id");
                var cycleId = File.ReadLines(cycleIdFile).ElementAt(0);

                _ = _orchestrationService.UpdateBatchHistoryTable("email", productFullName, "in-progress", cycleId);

                string sourcePath = productPath.Replace("INPUT", "OUTPUT");

                string destinationPath = "";

                if (sourcePath.Contains("ASB"))
                    destinationPath = _deliveryPaths.ASB;
                else if (sourcePath.Contains("TL-WR"))
                    destinationPath = _deliveryPaths.TLWR;
                else if (sourcePath.Contains("TL-WOR"))
                    destinationPath = _deliveryPaths.TLWOR;
                else if (sourcePath.Contains("TL-CC"))
                    destinationPath = _deliveryPaths.TLCC;
                else if (sourcePath.Contains("PF-VR"))
                    destinationPath = _deliveryPaths.PFVR;
                else if (sourcePath.Contains("PF-FR"))
                    destinationPath = _deliveryPaths.PFFR;
                else if (sourcePath.Contains("MG-WR"))
                    destinationPath = _deliveryPaths.MGWR;
                else if (sourcePath.Contains("MG-WOR"))
                    destinationPath = _deliveryPaths.MGWOR;
                else if (sourcePath.Contains("MG-EQT"))
                    destinationPath = _deliveryPaths.MGEQT;
                else if (sourcePath.Contains("MG-BBA"))
                    destinationPath = _deliveryPaths.MGBBA;

                Log.Information("Destination path for eml: " + destinationPath);

                bool status = await BackupandMove(sourcePath, destinationPath);

                
                if (!status)
                {
                    File.WriteAllText(productPath + "/delivery.error", "1");

                    var data = _orchestrationService.UpdateProgressTable("Fail", cycleId);

                    _ = _orchestrationService.UpdateBatchHistoryTable("email", productFullName, "failed", cycleId);
                    //_ = await MailProcess.UrlHit("");
                }
                else
                {
                    File.Move(productPath + "/delivery.inprogress", productPath + "/delivery.completed");
                    _ = _orchestrationService.UpdateBatchHistoryTable("email", productFullName, "completed", cycleId);
                }

                Log.Information("Als Delivery files moving and backup process has been completed.");
            }
            catch (Exception ex)
            {
                File.WriteAllText(productPath + "/delivery.error", "1");

                string cycleIdFile = productPath.ToLower().Contains("asb") ? Path.Combine(_etlPaths.CycleIdPath.Replace("{type}", "ASB-NM"), "asb_1_cycle.id") : productPath.ToLower().Contains("tl") ? Path.Combine(_etlPaths.CycleIdPath.Replace("{type}", "TL-NM"), "tl_1_cycle.id") : productPath.ToLower().Contains("mg") ? Path.Combine(_etlPaths.CycleIdPath.Replace("{type}", "MG-NM"), "mg_1_cycle.id") : Path.Combine(_etlPaths.CycleIdPath.Replace("{type}", "PF-NM"), "pf_1_cycle.id");
                var cycleId = File.ReadLines(cycleIdFile).ElementAt(0);

                var data = _orchestrationService.UpdateProgressTable("Fail", cycleId);

                _ = _orchestrationService.UpdateBatchHistoryTable("email","", "Fail", cycleId);

                Log.Information(ex, "Error while calling backup and move process..");
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
                Log.Information($"File moving process started for download folder..{statmentPath}");

                string[] inputFolders = Directory.GetDirectories(Path.Combine(statmentPath, "EML"));

                string cycleIdFile = statmentPath.ToLower().Contains("asb") ? Path.Combine(_etlPaths.CycleIdPath.Replace("{type}", "ASB-NM"), "asb_1_cycle.id") : statmentPath.ToLower().Contains("tl") ? Path.Combine(_etlPaths.CycleIdPath.Replace("{type}", "TL-NM"), "tl_1_cycle.id") : statmentPath.ToLower().Contains("mg") ? Path.Combine(_etlPaths.CycleIdPath.Replace("{type}", "MG-NM"), "mg_1_cycle.id") : Path.Combine(_etlPaths.CycleIdPath.Replace("{type}", "PF-NM"), "pf_1_cycle.id");
                var cycleId = File.ReadLines(cycleIdFile).ElementAt(0);
                string stmtdateFile = cycleIdFile.Replace("cycle.id", "stmt.date");

                string stmtdate = File.ReadLines(stmtdateFile).ElementAt(0);

                string backUpFolder = Path.Combine(statmentPath, "EML", "BACKUP", cycleId);

                var deliveryScheduler = _config.JobSettings["DeliverySchedular"];

                Directory.CreateDirectory(backUpFolder);

                foreach (var item in inputFolders)
                {
                    if (item.Contains("BACKUP"))
                        continue;

                    if (inputFolders[0]!=item)
                    {
                        Log.Information("Wait time interval initiated for the folder: "+item);

                        Thread.Sleep(deliveryScheduler.TimeInterval * 60000);

                        Log.Information("Wait time interval Completed for the folder: "+item+" wait time is:"+(deliveryScheduler.TimeInterval * 60000));

                    }

                    var files = Directory.GetFiles(item);

                    string batchFolder = Path.Combine(backUpFolder, new DirectoryInfo(item).Name);

                    Directory.CreateDirectory(batchFolder);

                    Log.Information("EML Destination path: " + destinationPath);

                    foreach (var file in files)
                    {
                        string backupFolder = Path.Combine(batchFolder, Path.GetFileName(file));

                        Log.Information("Backup folder for Delivery: " + batchFolder + "Destination folder: " + destinationPath);

                        CopyFile(file, backupFolder);

                     
                        if (file.Contains(".pdf"))
                        {
                            File.Move(file, Path.Combine(destinationPath, "attach1", Path.GetFileName(file)), true);
                        }
                        else if (file.Contains(".html"))
                        {
                            File.Move(file, Path.Combine(destinationPath, "html", Path.GetFileName(file)), true);

                        }
                    }

                    files = Directory.GetFiles(item);
                    
                    foreach (var file in files)
                    {
                        if (file.Contains(".dij"))
                        {
                            bool status = await DeliverySuppression(file, stmtdate,destinationPath);

                            string dijFolderPath = Path.Combine(destinationPath, "dij");

                            Log.Information("Suppression completed for delivery file " + file);

                            if(File.Exists(file))
                                File.Move(file, Path.Combine(destinationPath, "dij", Path.GetFileName(file)), true);
                        }
                    }

                    Log.Information("Delivery process completed " + item);

                    Directory.Delete(item);
                }

                if (Directory.GetDirectories(backUpFolder).Count() == 0)
                {
                    Log.Information("Empty folder: "+backUpFolder);
                    Directory.Delete(backUpFolder);
                    return true;
                }

                var backuprootfolder = Path.Combine(statmentPath, "EML", "BACKUP");

                Log.Information("Zip process initiated for " + backuprootfolder);

                var allFolders = Directory.GetDirectories(backuprootfolder);

                Log.Information($" Folders inside BACKUP folder is :{allFolders.Length}");

                foreach (var folder in allFolders)
                {
                    if (Directory.GetCreationTime(folder) < DateTime.Now.AddDays(-deliveryScheduler.FoldersIgnoreDays))
                    {
                        string backupscript = Path.Combine(_etlPaths.ETLHomePath, "TEMP", "gzip" + DateTime.Now.ToString("ddMMyyyyHHmmssfff") + ".sh");

                        try
                        {
                            var cycleFolder = new DirectoryInfo(folder).Name;

                            Log.Information($"Zip Process started for : {cycleFolder} created on {Directory.GetCreationTime(folder)}");

                            File.WriteAllText(backupscript, $"cd {Path.Combine(statmentPath, "EML", "BACKUP")}\ntar -zcvf {cycleFolder}.tar.gz {cycleFolder}\nrm -r {cycleFolder}");

                            string process = Helper.ProcessCommandLine(backupscript);

                            Log.Information("GZip process status:" + process);

                            File.Delete(backupscript);
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, "Error while making gzip script");
                        }
                    }
                }
                return true;

            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error while moving files to download folder: " + statmentPath);

                return false;
            }

        }

        public async Task<bool> DeliverySuppression(string filePath, string stmtDate, string destinationPath)
        {
            try
            {
                XDocument xml = XDocument.Load(filePath);

                var suppressedAccounts = await _orchestrationService.GetDeliverySuppressAccountNumbers(stmtDate);

                Log.Information("No of accounts to be suppressed at delivery count is :" + suppressedAccounts.Count());

                XElement doc = XElement.Parse(xml.ToString());

                //Suppress report
                IEnumerable<XElement> xDoc = XDocument.Parse(xml.ToString()).Descendants("document");
                var xmldata = (from item in xDoc
                               let accountNumber = (item.Elements("DDSDocValue")
                                   .Where(i => (string)i.Attribute("name") == "Account_Number").Select(i => i.Value)).FirstOrDefault()
                               let cisNumber = (item.Elements("DDSDocValue")
                                   .Where(i => (string)i.Attribute("name") == "CIS_Number").Select(i => i.Value)).FirstOrDefault()
                               let cycle_ID = (item.Elements("DDSDocValue")
                               .Where(i => (string)i.Attribute("name") == "Cycle_ID").Select(i => i.Value)).FirstOrDefault()
                               let delivery_Method = (item.Elements("DDSDocValue")
                                   .Where(i => (string)i.Attribute("name") == "Delivery_Method").Select(i => i.Value)).FirstOrDefault()
                               let email = (item.Elements("DDSDocValue")
                               .Where(i => (string)i.Attribute("name") == "Email").Select(i => i.Value)).FirstOrDefault()
                               let product_Description = (item.Elements("DDSDocValue")
                                   .Where(i => (string)i.Attribute("name") == "Product_Description").Select(i => i.Value)).FirstOrDefault()
                               let dob = (item.Elements("DDSDocValue")
                              .Where(i => (string)i.Attribute("name") == "DOB").Select(i => i.Value)).FirstOrDefault()
                               let statementType = (item.Elements("DDSDocValue")
                               .Where(i => (string)i.Attribute("name") == "Statement_Type").Select(i => i.Value)).FirstOrDefault()
                               let producttype = (item.Elements("DDSDocValue")
                                  .Where(i => (string)i.Attribute("name") == "Product_Type").Select(i => i.Value)).FirstOrDefault()

                               let product_Name = (item.Elements("DDSDocValue")
                                   .Where(i => (string)i.Attribute("name") == "Product_Name").Select(i => i.Value)).FirstOrDefault()
                               let productcode = (item.Elements("DDSDocValue")
                               .Where(i => (string)i.Attribute("name") == "Product_Code").Select(i => i.Value)).FirstOrDefault()

                               let productdescription = (item.Elements("DDSDocValue")
                                   .Where(i => (string)i.Attribute("name") == "Loan_Type").Select(i => i.Value)).FirstOrDefault()

                               let statement_Date = (item.Elements("DDSDocValue")
                              .Where(i => (string)i.Attribute("name") == "Statement_Date").Select(i => i.Value)).FirstOrDefault()

                               let file_Name = (item.Elements("DDSDocValue")
                              .Where(i => (string)i.Attribute("name") == "File_Name").Select(i => i.Value)).FirstOrDefault()

                               select new GenerateLog
                               {
                                   CycleID = cycle_ID,
                                   ProductName = product_Name,
                                   StatementDate = DateTime.ParseExact(stmtDate, "yyyyMMdd", null),
                                   CISNumber = cisNumber,
                                   FileName = file_Name,
                                   DOB = dob,
                                   Email = email,
                                   DeliveryMethod = delivery_Method,
                                   Remarks = "Suppresed at Delivery",
                                   Status = "SUCCESS",
                                   ProductType = producttype,
                                   AccountNumber = accountNumber,
                                   ProductDescription= productdescription,
                                   ProductCode= productcode,
                                   
                               }).ToList();


                    for (var i = 0; i < suppressedAccounts.Count(); i++)
                    {
                        doc.Descendants("document").Elements("DDSDocValue").Where(r => r.Value == suppressedAccounts[i] && (string)r.Attribute("name") == "Customer_Number").Select(p => p.Parent).Remove();

                        Log.Information("Customer number supressed in delivery " + suppressedAccounts[i]);

                        if (suppressedAccounts[i] != null)
                        {
                            var accountData = xmldata.FirstOrDefault(x => x.AccountNumber == suppressedAccounts[i]);

                            if (accountData != null)
                            {
                                Log.Information("Suppressed account from DIJ: "+accountData.AccountNumber);

                                var suppressionReport = new SuppressionReport
                                {
                                    AccountNumber = suppressedAccounts[i],
                                    CISNumber = accountData.CISNumber,
                                    CycleID = accountData.CycleID,
                                    DeliveryMethod = accountData.DeliveryMethod,
                                    EmailAddress = accountData.Email,
                                    Dob = accountData.DOB,
                                    FileName = accountData.FileName,
                                    ProductName = accountData.ProductName,
                                    StatementDate = accountData.StatementDate,
                                    Status = accountData.Status,
                                    Remarks = "Suppressed at Delivery",
                                    ProductType = accountData.ProductType,
                                    Description=accountData.ProductDescription,
                                    ProductCode= accountData.ProductCode,

                                };

                                File.Delete(Path.Combine(destinationPath, "attach1", Path.GetFileName(accountData.FileName)));
                                File.Delete(Path.Combine(destinationPath, "html", Path.GetFileName(accountData.FileName.Replace(".pdf", ".html"))));

                                _ = await _orchestrationService.SaveSuppressionRecord(suppressionReport);
                            }
                        }
                    }

                File.Delete(filePath);

                Log.Information("No of customers in DIJ is:"+doc.Descendants("document").Count());

                if (doc.Descendants("document").Count() != 0)
                {

                    File.WriteAllText(filePath, "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"no\"?>\r\n<!DOCTYPE eGAD SYSTEM \"eGAD.Dtd\">\n"+doc.ToString());
                }
                Log.Information("Delivery suppression completed " + filePath);

                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error while delivery suppression");
                return false;
            }
        }
    }
}
