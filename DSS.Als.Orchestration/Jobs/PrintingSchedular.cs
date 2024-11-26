using Common.MicroBatch.Repository;
using DSS.Als.Orchestration.Models;
using DSS.Als.Orchestration.CommonProcess;
using DSS.Orchestration.CommonProcess;
using Microsoft.Extensions.Options;
using Quartz;
using Renci.SshNet;
using Serilog;
using System.Security.Cryptography;

namespace RHB.Als.Orchestration.Jobs
{
  
    public class PrintingSchedular : IJob
    {
        private readonly StatementGeneration _statementGeneration;
        private readonly PrintSettings _printSettings;
        private readonly EtlPaths _etlPaths;
        private readonly IOrchestrationService _orchestrationService;

        public PrintingSchedular(IOptions<PrintSettings> printSettings, IOptions<StatementGeneration> statmentGeneration, IOrchestrationService orchestrationService, IOptions<EtlPaths> etlPaths)
        {
            _orchestrationService = orchestrationService;
            _statementGeneration = statmentGeneration.Value;
            _printSettings = printSettings.Value;
            _etlPaths = etlPaths.Value;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            try
            {
                string[] stmtList = Directory.GetDirectories(_statementGeneration.AlsInfo.InputFilePath.RootPath.Replace("{type}", "ASB-NM"));

                foreach (var item in stmtList)
                {
                    string[] readyFile = Directory.GetFiles(item, "print.ready");

                    if (readyFile.Length > 0)
                        PrintProcess(item);
                }

                stmtList = Directory.GetDirectories(_statementGeneration.AlsInfo.InputFilePath.RootPath.Replace("{type}", "TL-NM"));

                foreach (var item in stmtList)
                {
                    string[] readyFile = Directory.GetFiles(item, "print.ready");

                    if (readyFile.Length > 0)
                        PrintProcess(item);
                }

                stmtList = Directory.GetDirectories(_statementGeneration.AlsInfo.InputFilePath.RootPath.Replace("{type}", "PF-NM"));

                foreach (var item in stmtList)
                {
                    string[] readyFile = Directory.GetFiles(item, "print.ready");

                    if (readyFile.Length > 0)
                        PrintProcess(item);
                }

                stmtList = Directory.GetDirectories(_statementGeneration.AlsInfo.InputFilePath.RootPath.Replace("{type}", "MG-NM"));

                foreach (var item in stmtList)
                {
                    string[] readyFile = Directory.GetFiles(item, "print.ready");

                    if (readyFile.Length > 0)
                        PrintProcess(item);
                }

            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error in print Scheduler job. ");
            }
            await Task.CompletedTask;
        }

        public void PrintProcess(string productPath)
        {
            string stmtType = new DirectoryInfo(productPath).Name;

            string productFullName = Helper.GetFullNames(stmtType);

            string outputPath = productPath.Replace("INPUT", "OUTPUT");

            File.Move(productPath + "/print.ready", productPath + "/print.inprogress");

            try
            {
                Log.Information("Print initiated");
                string cycleIdFile = productPath.ToLower().Contains("asb") ? Path.Combine(_etlPaths.CycleIdPath.Replace("{type}", "ASB-NM"), "asb_1_cycle.id") : productPath.ToLower().Contains("tl") ? Path.Combine(_etlPaths.CycleIdPath.Replace("{type}", "TL-NM"), "tl_1_cycle.id") : productPath.ToLower().Contains("mg") ? Path.Combine(_etlPaths.CycleIdPath.Replace("{type}", "MG-NM"), "mg_1_cycle.id") : Path.Combine(_etlPaths.CycleIdPath.Replace("{type}", "PF-NM"), "pf_1_cycle.id");
                var cycleId = File.ReadLines(cycleIdFile).ElementAt(0);
                string reportFileInputPath = FilesProcess(Path.Combine(outputPath, "PRT"));

                if (reportFileInputPath == "Error")
                {
                    //_ = MailProcess.UrlHit("");

                    File.WriteAllText(productPath + "/print.error", "1");

                    var data = _orchestrationService.UpdateProgressTable("Fail", cycleId);

                    int retry = 1;

                    while (data == null && retry < _statementGeneration.DbRetryAttempts)
                    {
                        Thread.Sleep(_statementGeneration.DbRetryWaitTime*1000);

                        Log.Information($"Retry for progress table print db fail case {retry} id:"+cycleId);

                        data = _orchestrationService.UpdateProgressTable("Fail", cycleId);

                        retry++;
                    }

                    _ = _orchestrationService.UpdateBatchHistoryTable("hardcopy", productFullName, "failed", cycleId);

                    _ = Backup(Path.Combine(outputPath, "PRT"));

                    return;
                }
                else if (reportFileInputPath == "No Files" || reportFileInputPath == "")
                {
                    //File.WriteAllText(productPath + "/print.completed", "0");
                    File.Move(productPath + "/print.inprogress", productPath + "/print.completed");

                    _ = _orchestrationService.UpdateBatchHistoryTable("hardcopy", productFullName, "completed", cycleId);

                    Log.Information("Print Process completed");

                    return;
                }
                Log.Information("Files backup initiated..");

                _ = Backup(Path.Combine(outputPath, "PRT"));

                Log.Information("Files backup completed..");

                string[] prtFiles = reportFileInputPath.Split(',');

                if (prtFiles.Length > 1)
                {
                    foreach (var item in prtFiles)
                    {
                        
                        Log.Information($"Summary report for {item} is initiated.");

                        var status = Helper.HardCopySummaryReport(item, item.Replace("_00001_", "_"));

                        if (!status) {

                            File.WriteAllText(productPath + "/print.error", "1");

                            var data = _orchestrationService.UpdateProgressTable("Fail", cycleId);

                            _ = _orchestrationService.UpdateBatchHistoryTable("hardcopy", productFullName, "failed", cycleId);

                            _ = Backup(Path.Combine(outputPath, "PRT"));
                            return;
                        }
                        File.Delete(item);
                    }
                }
                else
                {
                    Log.Information($"Summary report for {prtFiles[0]} is initiated.");

                    var status = Helper.HardCopySummaryReport(prtFiles[0], prtFiles[0].Replace("_00001_", "_"));

                    if (!status)
                    {

                        File.WriteAllText(productPath + "/print.error", "1");

                        var data = _orchestrationService.UpdateProgressTable("Fail", cycleId);

                        _ = _orchestrationService.UpdateBatchHistoryTable("hardcopy", productFullName, "failed", cycleId);

                        _ = Backup(Path.Combine(outputPath, "PRT"));
                        return;
                    }

                    File.Delete(prtFiles[0]);
                }

                Log.Information("PGP Encryption and FTP initiated..");

                try
                {
                    string[] tempFiles = Directory.GetFiles(Path.Combine(outputPath, "PRT", "TEMP"));

                    Directory.CreateDirectory(Path.Combine(outputPath, "PRT", "ENC"));
                    Directory.CreateDirectory(Path.Combine(outputPath, "PRT", "TEMP"));

                    string keypath = _printSettings.PGPPublicKeyPath;

                    foreach (var file in tempFiles)
                    {
                        string fileName = Path.GetFileName(file);
                        bool status = EncFileProcess.EncryptAFile(file, Path.Combine(outputPath, "PRT", "ENC", fileName + ".PGP"), keypath);
                        Log.Information($"Encryption File {file} status: {status}");

                        if (!status)
                        {
                            _ = BackupEncFiles(Path.Combine(outputPath, "PRT", "ENC"));

                            _ = _orchestrationService.UpdateBatchHistoryTable("hardcopy", productFullName, "failed", cycleId);

                            File.WriteAllText(productPath + "/print.error", "1");

                            Log.Information("Print Process Failed");

                            return;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "error in encryption process");

                    _ = BackupEncFiles(Path.Combine(outputPath, "PRT", "ENC"));

                    _ = _orchestrationService.UpdateBatchHistoryTable("hardcopy", productFullName, "failed", cycleId);

                    File.WriteAllText(productPath + "/print.error", "1");

                    Log.Information("Print Process Failed");

                    return;
                }

                //For SFTP
                try
                {
                    string[] encFiles = Directory.GetFiles(Path.Combine(outputPath, "PRT", "ENC"));
                    
                    Log.Information("SFTP Process Initiated..");

                    var destinationPath = "";

                    if (_printSettings.SftpSettings.PV1)
                        destinationPath = _printSettings.SftpSettings.PV1SFTP;

                    else if (_printSettings.SftpSettings.PV2)
                        destinationPath = _printSettings.SftpSettings.PV2SFTP;

                    Log.Information("SFTP destination path is:"+destinationPath);

                    if (destinationPath.Trim() == "")
                    {

                        _ = BackupEncFiles(Path.Combine(outputPath, "PRT", "ENC"));

                        _ = _orchestrationService.UpdateBatchHistoryTable("hardcopy", productFullName, "failed", cycleId);

                        File.WriteAllText(productPath + "/print.error", "1");

                        Log.Information("SFTP Destination Path not found.");

                        return;
                    }

                    foreach (var file in encFiles)
                    {
                        string fileName = Path.GetFileName(file);
                        SftpClient client = new SftpClient(_printSettings.SftpSettings.SftpServerIp, _printSettings.SftpSettings.SftpServerPort, _printSettings.SftpSettings.UserName, _printSettings.SftpSettings.Password);
                        client.Connect();

                        if (client.IsConnected)
                        {
                            using (Stream fileStream = File.OpenRead(file))
                            {
                                client.UploadFile(fileStream, Path.Combine(destinationPath, fileName));
                            }
                        }

                        client.Disconnect();
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "error while uploding a file to the sftp folder");

                    _ = BackupEncFiles(Path.Combine(outputPath, "PRT", "ENC"));

                    _ = _orchestrationService.UpdateBatchHistoryTable("hardcopy", productFullName, "failed", cycleId);

                    File.WriteAllText(productPath + "/print.error", "1");

                    Log.Information("Print Process Failed");

                    return;
                }

                //SFTP Ended
                try
                {
                    string[] files = Directory.GetFiles(Path.Combine(outputPath, "PRT", "ENC"));

                    foreach (var item in files)
                    {
                        string fileName = Path.GetFileName(item);

                        string sourceFileChecksum = GetChecksum(HashingAlgoTypes.SHA512,item);

                        Log.Information($"{item} checksum value is: "+sourceFileChecksum);

                        SftpClient client = new SftpClient(_printSettings.SftpSettings.SftpServerIp, _printSettings.SftpSettings.SftpServerPort, _printSettings.SftpSettings.UserName, _printSettings.SftpSettings.Password);

                        client.Connect();

                        if (client.IsConnected)
                        {
                            var destinationPath = "";

                            if (_printSettings.SftpSettings.PV1)
                                destinationPath = _printSettings.SftpSettings.PV1SFTP;

                            else if (_printSettings.SftpSettings.PV2)
                                destinationPath = _printSettings.SftpSettings.PV2SFTP;

                            using (Stream fileStream = File.OpenWrite(Path.Combine(_etlPaths.ETLHomePath,"TEMP", fileName)))
                            {
                                client.DownloadFile(Path.Combine(destinationPath, fileName), fileStream);

                            }
                        }

                        client.Disconnect();

                        string sftpfilechecksum = GetChecksum(HashingAlgoTypes.SHA512, Path.Combine(_etlPaths.ETLHomePath, "TEMP", fileName));

                        Log.Information("sftp file checksum value: " + sftpfilechecksum);

                        File.Delete(Path.Combine(_etlPaths.ETLHomePath, "TEMP", fileName));

                        if (sourceFileChecksum == sftpfilechecksum)
                            Log.Information("Checksum matched for the file:"+item);
                        else
                        {
                            Log.Information("Checksum not matched for the file:"+item);

                            _ = BackupEncFiles(Path.Combine(outputPath, "PRT", "ENC"));

                            _ = _orchestrationService.UpdateBatchHistoryTable("hardcopy", productFullName, "failed", cycleId);

                            File.Move(productPath + "/print.inprogress", productPath + "/print.error");

                            Log.Information("Print Process Failed");

                            return;
                        }
                    }

                }
                catch (Exception ex) {

                    Log.Error(ex, "Error in SFTP verification Process");

                    _ = BackupEncFiles(Path.Combine(outputPath, "PRT", "ENC"));

                    _ = _orchestrationService.UpdateBatchHistoryTable("hardcopy", productFullName, "failed", cycleId);

                    File.Move(productPath + "/print.inprogress", productPath + "/print.error");

                    Log.Information("Print Process Failed");

                    return;
                }

                _ = BackupEncFiles(Path.Combine(outputPath, "PRT","ENC"));

                Log.Information("Print Process completed");

                _ = _orchestrationService.UpdateBatchHistoryTable("hardcopy", productFullName, "completed", cycleId);

                File.Move(productPath + "/print.inprogress", productPath + "/print.completed");
                //File.WriteAllText(productPath + "/print.completed", "0");
            }
            catch (Exception ex)
            {
                //_ = MailProcess.UrlHit("");

                Log.Error(ex, "Error in Print files Process");

                File.WriteAllText(productPath + "/print.error", "1");

                string cycleIdFile = productPath.ToLower().Contains("asb") ? Path.Combine(_etlPaths.CycleIdPath.Replace("{type}", "ASB-NM"), "asb_1_cycle.id") : productPath.ToLower().Contains("tl") ? Path.Combine(_etlPaths.CycleIdPath.Replace("{type}", "TL-NM"), "tl_1_cycle.id") : productPath.ToLower().Contains("mg") ? Path.Combine(_etlPaths.CycleIdPath.Replace("{type}", "MG-NM"), "mg_1_cycle.id") : Path.Combine(_etlPaths.CycleIdPath.Replace("{type}", "PF-NM"), "pf_1_cycle.id");
                var cycleId = File.ReadLines(cycleIdFile).ElementAt(0);

                var data = _orchestrationService.UpdateProgressTable("Fail", cycleId);

                int retry = 1;

                while (data == null && retry < _statementGeneration.DbRetryAttempts)
                {
                    Thread.Sleep(_statementGeneration.DbRetryWaitTime*1000);

                    Log.Information($"Retry for progress table print db fail case {retry} id:"+cycleId);

                    data = _orchestrationService.UpdateProgressTable("Fail", cycleId);

                    retry++;
                }

                _ = _orchestrationService.UpdateBatchHistoryTable("hardcopy", productFullName, "failed", cycleId);

                _ = Backup(Path.Combine(outputPath, "PRT"));

                _ = BackupEncFiles(Path.Combine(outputPath, "PRT", "ENC"));
            }
        }

        public async Task<bool> Backup(string sourcePath)
        {
            try
            {
                Log.Information($"BackUp Process initiated for {sourcePath}");

                var batchDirectorys = Directory.GetDirectories(sourcePath, "BATCH_*");

                string cycleIdFile = sourcePath.ToLower().Contains("asb") ? Path.Combine(_etlPaths.CycleIdPath.Replace("{type}", "ASB-NM"), "asb_1_cycle.id") : sourcePath.ToLower().Contains("tl") ? Path.Combine(_etlPaths.CycleIdPath.Replace("{type}", "TL-NM"), "tl_1_cycle.id") : sourcePath.ToLower().Contains("mg") ? Path.Combine(_etlPaths.CycleIdPath.Replace("{type}", "MG-NM"), "mg_1_cycle.id") : Path.Combine(_etlPaths.CycleIdPath.Replace("{type}", "PF-NM"), "pf_1_cycle.id");
                var cycleId = File.ReadLines(cycleIdFile).ElementAt(0);

                string backUpFolder = Path.Combine(sourcePath, "BACKUP", cycleId);

                if (batchDirectorys.Count() == 0) {

                    Log.Information("Empty Folder: "+sourcePath);

                    processDirectory(sourcePath);

                    return true;
                    
                }

                Directory.CreateDirectory(backUpFolder);

                foreach (var item in batchDirectorys)
                {
                    var files = Directory.GetFiles(item);

                    string batchFolder = Path.Combine(backUpFolder, new DirectoryInfo(item).Name);

                    Log.Information("PRT Backup folder path: " + batchFolder);

                    Directory.CreateDirectory(batchFolder);

                    foreach (var file in files)
                    {
                        string backup = Path.Combine(batchFolder, Path.GetFileName(file));

                        Log.Information("PRT back file Path: " + backup);

                        File.Move(file, backup);
                    }
                    Directory.Delete(item);
                }

                Log.Information("Zip process initiated for " + backUpFolder);

                string backupscript = Path.Combine(_etlPaths.ETLHomePath, "TEMP", "gzip" + DateTime.Now.ToString("ddMMyyyyHHmmssfff") + ".sh");

                try
                {

                    File.WriteAllText(backupscript, $"cd {Path.Combine(sourcePath, "BACKUP")}\ntar -zcvf {cycleId}.tar.gz {cycleId}\nrm -r {cycleId}");

                    string process = Helper.ProcessCommandLine(backupscript);

                    Log.Information("GZip process status:" + process);

                    File.Delete(backupscript);

                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error while making gzip script");
                }

                return true;

            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error while copying files : " + sourcePath);

                return false;
            }

        }

        private static void processDirectory(string startLocation)
        {
            foreach (var directory in Directory.GetDirectories(startLocation,"BATCH_*"))
            {
                processDirectory(directory);
                if (Directory.GetFiles(directory).Length == 0 && Directory.GetDirectories(directory).Length == 0)
                {
                    Directory.Delete(directory, false);
                }
            }
        }

        public async Task<bool> BackupEncFiles(string sourcePath)
        {
            try
            {
                Log.Information($"BackUp Encfiles Process initiated for {sourcePath}");

                string cycleIdFile = sourcePath.ToLower().Contains("asb") ? Path.Combine(_etlPaths.CycleIdPath.Replace("{type}", "ASB-NM"), "asb_1_cycle.id") : sourcePath.ToLower().Contains("tl") ? Path.Combine(_etlPaths.CycleIdPath.Replace("{type}", "TL-NM"), "tl_1_cycle.id") : sourcePath.ToLower().Contains("mg") ? Path.Combine(_etlPaths.CycleIdPath.Replace("{type}", "MG-NM"), "mg_1_cycle.id") : Path.Combine(_etlPaths.CycleIdPath.Replace("{type}", "PF-NM"), "pf_1_cycle.id");
                var cycleId = File.ReadLines(cycleIdFile).ElementAt(0);

                string backUpFolder = Path.Combine(sourcePath, "BACKUP", cycleId);

                Directory.CreateDirectory(backUpFolder);
                    
                var files = Directory.GetFiles(sourcePath);

                    Log.Information("PRT Backup folder path: " + backUpFolder);

                    foreach (var file in files)
                    {
                        string backup = Path.Combine(backUpFolder, Path.GetFileName(file));

                        Log.Information("PRT back file Path: " + backup);

                        File.Move(file, backup);

                    }

                if (Directory.GetFiles(backUpFolder).Length == 0)
                {
                    Log.Information("Empty folder: "+backUpFolder);
                    
                    if(!backUpFolder.Contains("BACKUP"))
                        Directory.Delete(backUpFolder);
                    return true;
                }

                Log.Information("Zip process initiated for " + backUpFolder);

                string backupscript = Path.Combine(_etlPaths.ETLHomePath, "TEMP", "gzip" + DateTime.Now.ToString("ddMMyyyyHHmmssfff") + ".sh");

                try
                {

                    File.WriteAllText(backupscript, $"cd {Path.Combine(sourcePath, "BACKUP")}\ntar -zcvf {cycleId}.tar.gz {cycleId}\nrm -r {cycleId}");

                    string process = Helper.ProcessCommandLine(backupscript);

                    Log.Information("GZip process status:" + process);

                    File.Delete(backupscript);

                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error while making gzip script");
                    return false;
                }

                return true;

            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error while copying files : " + sourcePath);

                return false;
            }

        }

        public string FilesProcess(string prtPath)
        {
          
            try
            {
                string consolidatedLtxtfile = "", consolidatedOtxtfile = "";

                Log.Information("Prt File process root folder: " + prtPath);

                var files = Directory.GetFiles(prtPath, "*.txt", SearchOption.AllDirectories).Where(d => (d.Contains("BATCH") && !d.Contains("BACKUP"))).ToArray();

                var localfiles = Directory.GetFiles(prtPath, "*_l_*.txt", SearchOption.AllDirectories).Where(d => (d.Contains("BATCH") && !d.Contains("BACKUP"))).ToArray();
               
                var oversisfiles = Directory.GetFiles(prtPath, "*_o_*.txt", SearchOption.AllDirectories).Where(d => (d.Contains("BATCH") && !d.Contains("BACKUP"))).ToArray();

                Array.Sort(localfiles);
                Array.Sort(oversisfiles);

                if (localfiles.Length > 0)
                {
                    
                    consolidatedLtxtfile = File.ReadAllLines(localfiles[0]).ElementAt(1).Split('|')[27].Replace(".afp", "");
                    
                    Log.Information("Summery report L input file name: "+consolidatedLtxtfile+": "+localfiles[0]);
                }
                else
                {
                    Log.Information("PRT L txt files zero in " + prtPath);
                    
                }

                if (oversisfiles.Length > 0)
                {
                    consolidatedOtxtfile = File.ReadAllLines(oversisfiles[0]).ElementAt(1).Split('|')[27].Replace(".afp", "");
                    
                    Log.Information("Summery report O input file name: "+consolidatedOtxtfile+ ": "+oversisfiles[0]);
                }
                else
                {
                    Log.Information("PRT O txt files zero in " + prtPath);
                    
                }

                if (consolidatedLtxtfile == "" && consolidatedOtxtfile == "")
                    return "No Files";

                string prtOtrgtPath = Path.Combine(prtPath, "TEMP", consolidatedOtxtfile + ".txt");
                
                string prtLtrgtPath = Path.Combine(prtPath, "TEMP", consolidatedLtxtfile + ".txt");

                for (var i = 0; i < files.Count(); i++)
                {
                    var oldfilename = Path.GetFileNameWithoutExtension(files[i]);

                    var newfilename = File.ReadAllLines(files[i]).ElementAt(1).Split('|')[27].Replace(".afp", "");

                    var getpath = Path.GetDirectoryName(files[i]);

                    //change
                    var data = File.ReadAllLines(files[i]).Skip(1);

                    if(files[i].Contains("_l_"))
                        File.AppendAllLines(prtLtrgtPath, data.ToList());
                    else
                        File.AppendAllLines(prtOtrgtPath, data.ToList());

                    File.Move(getpath + "/" + oldfilename + ".txt", getpath + "/" + newfilename + ".txt");

                    File.Move(getpath + "/" + oldfilename + ".afp", getpath + "/" + newfilename + ".afp");

                    var txtFile = getpath + "/" + newfilename + ".txt";
                    var afpFile = getpath + "/" + newfilename + ".afp";

                    string destinationPath = new FileInfo(prtOtrgtPath).Directory.FullName;
                    Directory.CreateDirectory(destinationPath);

                    if (!File.Exists(Path.Combine(destinationPath, Path.GetFileName(afpFile))))
                        File.Copy(afpFile, Path.Combine(destinationPath, Path.GetFileName(afpFile)));

                }

                if(consolidatedLtxtfile != "" && consolidatedOtxtfile == "")
                    return Path.Combine(prtPath, "TEMP", consolidatedLtxtfile + ".txt");
                else if(consolidatedLtxtfile == "" && consolidatedOtxtfile != "")
                    return Path.Combine(prtPath, "TEMP", consolidatedOtxtfile + ".txt");
                else if (consolidatedLtxtfile != "" && consolidatedOtxtfile != "")
                    return Path.Combine(prtPath, "TEMP", consolidatedLtxtfile + ".txt") +","+ Path.Combine(prtPath, "TEMP", consolidatedOtxtfile + ".txt");

                return "";
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error while processing PRT files");
                return "Error";
            }
        }


        public static void CopyFile(string inputFilePath, string outputFilePath)
        {
            try
            {
                int bufferSize = 1024 * 1024;

                using (FileStream fileStream = new FileStream(outputFilePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite))
                {
                    FileStream fs = new FileStream(inputFilePath, FileMode.Open, FileAccess.ReadWrite);
                    fileStream.SetLength(fs.Length);
                    int bytesRead = -1;
                    byte[] bytes = new byte[bufferSize];

                    while ((bytesRead = fs.Read(bytes, 0, bufferSize)) > 0)
                    {
                        fileStream.Write(bytes, 0, bytesRead);
                    }
                    fs.Close();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error while copying");
            }
        }

        //Print Verification checksum
        public static string GetChecksum(HashingAlgoTypes hashingAlgoType, string filename)
        {

            using (var hasher = HashAlgorithm.Create(hashingAlgoType.ToString()))
            {
                using (var stream = File.OpenRead(filename))
                {
                    var hash = hasher.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "");
                }
            }
        }
        public enum HashingAlgoTypes
        {
            MD5,
            SHA1,
            SHA256,
            SHA384,
            SHA512
        }
    }
}
