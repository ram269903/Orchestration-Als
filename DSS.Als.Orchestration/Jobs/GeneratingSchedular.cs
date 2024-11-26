using Common.MicroBatch.Repository;
using Common.MicroBatch.Repository.Model;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Quartz;
using Serilog;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Net;
using System.Text;
using System.Xml.Linq;
using DSS.Als.Orchestration.Models;
using DSS.Als.Orchestration.CommonProcess;
using System.Configuration;

namespace RHB.Als.Orchestration.Jobs
{
    public class GeneratingSchedular : IJob
    {
        private readonly StatementGeneration _statementGeneration;
        private readonly IOrchestrationService _microBatchService;
        private readonly EtlPaths _etlPaths;
        private readonly HAConfig _haConfig;
        string processServer = "";

        public GeneratingSchedular(IOptions<StatementGeneration> statmentGeneration, IOptions<EtlPaths> etlPaths, IOptions<HAConfig> haConfig, IOrchestrationService microBatchService)
        {
            _microBatchService = microBatchService;
            _statementGeneration = statmentGeneration.Value;
            _etlPaths = etlPaths.Value;
            _haConfig = haConfig.Value;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            try
            {
                string[] stmtList = Directory.GetDirectories(_statementGeneration.AlsInfo.InputFilePath.RootPath.Replace("{type}", "ASB-NM"));

                foreach (var item in stmtList)
                {
                    string[] readyFile = Directory.GetFiles(item, "microbatch.ready");

                    if (readyFile.Length > 0)
                    {
                        var serverAvailable = GetServerAvailability(item);

                        if (serverAvailable)
                        {
                            processServer = processServer.Trim() == "Server1" ? "sg01" : processServer.Trim() == "Server2" ? "sg02" : processServer.Trim() == "Server3" ? "sg03" : processServer;

                            GenerateProces(item);
                        }
                    }
                }

                stmtList = Directory.GetDirectories(_statementGeneration.AlsInfo.InputFilePath.RootPath.Replace("{type}", "TL-NM"));

                foreach (var item in stmtList)
                {
                    string[] readyFile = Directory.GetFiles(item, "microbatch.ready");

                    if (readyFile.Length > 0)
                    {
                        var serverAvailable = GetServerAvailability(item);

                        if (serverAvailable)
                        {
                            processServer = processServer.Trim() == "Server1" ? "sg01" : processServer.Trim() == "Server2" ? "sg02" : processServer.Trim() == "Server3" ? "sg03" : processServer;

                            GenerateProces(item);
                        }
                    }
                }

                stmtList = Directory.GetDirectories(_statementGeneration.AlsInfo.InputFilePath.RootPath.Replace("{type}", "PF-NM"));

                foreach (var item in stmtList)
                {
                    string[] readyFile = Directory.GetFiles(item, "microbatch.ready");

                    if (readyFile.Length > 0)
                    {
                        var serverAvailable = GetServerAvailability(item);

                        if (serverAvailable)
                        {
                            processServer = processServer.Trim() == "Server1" ? "sg01" : processServer.Trim() == "Server2" ? "sg02" : processServer.Trim() == "Server3" ? "sg03" : processServer;

                            GenerateProces(item);
                        }
                    }
                }

                stmtList = Directory.GetDirectories(_statementGeneration.AlsInfo.InputFilePath.RootPath.Replace("{type}", "MG-NM"));

                foreach (var item in stmtList)
                {
                    string[] readyFile = Directory.GetFiles(item, "microbatch.ready");

                    if (readyFile.Length > 0)
                    {
                        var serverAvailable = GetServerAvailability(item);

                        if (serverAvailable)
                        {
                            processServer = processServer.Trim() == "Server1" ? "sg01" : processServer.Trim() == "Server2" ? "sg02" : processServer.Trim() == "Server3" ? "sg03" : processServer;

                            GenerateProces(item);
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error in Generate Scheduler job. ");
            }

            await Task.CompletedTask;

        }

        public async void GenerateProces(string productPath)
        {
            string stmtType = new DirectoryInfo(productPath).Name;

            string ematchFolder = productPath.ToLower().Contains("asb") ? _etlPaths.EmatchFolder.Replace("{type}", "ASB-NM") : productPath.ToLower().Contains("tl") ? _etlPaths.EmatchFolder.Replace("{type}", "TL-NM") : productPath.ToLower().Contains("pf") ? _etlPaths.EmatchFolder.Replace("{type}", "PF-NM") : _etlPaths.EmatchFolder.Replace("{type}", "MG-NM");

            string productFullName = Helper.GetFullNames(stmtType);

            try
            {
                Log.Information("Statements generation started..."+productFullName);

                string opsTemplatePath = "";

                var data = File.ReadLines(productPath + "/microbatch.ready").ElementAt(0);

                File.Move(productPath + "/microbatch.ready", productPath + "/generate.inprogress", true);
                Log.Information("Inside micro batch file value: " + data);

                string cycleIdFile = productPath.ToLower().Contains("asb") ? Path.Combine(_etlPaths.CycleIdPath.Replace("{type}", "ASB-NM"), "asb_1_cycle.id") : productPath.ToLower().Contains("tl") ? Path.Combine(_etlPaths.CycleIdPath.Replace("{type}", "TL-NM"), "tl_1_cycle.id") : productPath.ToLower().Contains("mg") ? Path.Combine(_etlPaths.CycleIdPath.Replace("{type}", "MG-NM"), "mg_1_cycle.id") : Path.Combine(_etlPaths.CycleIdPath.Replace("{type}", "PF-NM"), "pf_1_cycle.id");

                var cycleId = File.ReadLines(cycleIdFile).ElementAt(0);

                if (data == "1")
                {
                    var id = _microBatchService.UpdateProgressTable("Fail", cycleId);

                    int retry = 1;

                    while (id == null && retry < _statementGeneration.DbRetryAttempts)
                    {
                        Thread.Sleep(_statementGeneration.DbRetryWaitTime*1000);

                        Log.Information($"Retry for progress table generate db fail case {retry} id:"+cycleId);

                        id = _microBatchService.UpdateProgressTable("Fail", cycleId);

                        retry++;
                    }

                    _ = await _microBatchService.UpdateBatchHistoryTable("generation", productFullName, processServer+":failed", cycleId);

                    File.WriteAllText(productPath + "/generate.error", data);

                    string path = Path.Combine(ematchFolder, "MATCH", new DirectoryInfo(productPath).Name);
                    var freason = await Backup(path);

                    Log.Error("Backup status for ematch failure backup: " + freason);

                    string e2path = Path.Combine(ematchFolder, "EXTRACT2", new DirectoryInfo(productPath).Name);

                    var e2reason = await Backup(e2path);

                    Log.Error("Backup status for e2 folder failure backup: " + e2reason);

                    return;

                }
                else
                {
                    var alsInfo = _statementGeneration.AlsInfo;

                    opsTemplatePath = alsInfo.OpsFilePath;

                    bool generateStatus = await InvokeGenerateProcess(productPath, alsInfo);

                    Log.Information("Main thread exited with : " + generateStatus);

                    if (generateStatus)
                        Log.Information("Generation process completed for folder: " + productPath);
                    else
                    {
                        //_ = await MailProcess.UrlHit("");

                        Log.Information("Generation process failed for folder: " + productPath);

                        File.WriteAllText(productPath + "/generate.error", "1");

                        var id = _microBatchService.UpdateProgressTable("Fail", cycleId);

                        int retry = 1;

                        while (id == null && retry < _statementGeneration.DbRetryAttempts)
                        {
                            Thread.Sleep(_statementGeneration.DbRetryWaitTime*1000);

                            Log.Information($"Retry for progress table generate db fail case {retry} id:"+cycleId);

                            id = _microBatchService.UpdateProgressTable("Fail", cycleId);

                            retry++;
                        }

                        _ =await _microBatchService.UpdateBatchHistoryTable("generation", productFullName, processServer+":failed", cycleId);


                        string path = Path.Combine(ematchFolder, "MATCH", new DirectoryInfo(productPath).Name);

                        var freason = await Backup(path);

                        Log.Error("Backup status for ematch failure backup: " + freason);

                        string e2path = Path.Combine(ematchFolder, "EXTRACT2", new DirectoryInfo(productPath).Name);

                        var e2reason = await Backup(e2path);

                        Log.Error("Backup status for e2 folder failure backup: " + e2reason);

                        string inputrootPath = productPath.ToLower().Contains("asb") ? _statementGeneration.AlsInfo.InputFilePath.RootPath.Replace("{type}", "ASB-NM") : productPath.ToLower().Contains("tl") ? _statementGeneration.AlsInfo.InputFilePath.RootPath.Replace("{type}", "TL-NM") : productPath.ToLower().Contains("pf") ? _statementGeneration.AlsInfo.InputFilePath.RootPath.Replace("{type}", "PF-NM") : _statementGeneration.AlsInfo.InputFilePath.RootPath.Replace("{type}", "MG-NM");

                        string genout = Path.Combine(inputrootPath.Replace("INPUT", "OUTPUT"), new DirectoryInfo(productPath).Name, "ARS");

                        var genoutreason = await Backup(genout);

                        Log.Error("Backup status for gen out ars folder failure backup: " + genoutreason);

                        genout = Path.Combine(inputrootPath.Replace("INPUT", "OUTPUT"), new DirectoryInfo(productPath).Name, "EML");

                        genoutreason = await Backup(genout);

                        Log.Error("Backup status for genout eml folder failure backup: " + genoutreason);

                        genout = Path.Combine(inputrootPath.Replace("INPUT", "OUTPUT"), new DirectoryInfo(productPath).Name, "PRT");

                        genoutreason = await Backup(genout);

                        Log.Error("Backup status for genout prt folder failure backup: " + genoutreason);

                        return;
                    }
                }

                File.Move(productPath + "/generate.inprogress", productPath + "/generate.completed", true);

                string tarstatus = TarProcess(productPath, cycleId, true);

                string nmtarstatus = TarProcess(productPath.ToLower().Contains("asb") ? _etlPaths.CycleIdPath.Replace("{type}", "ASB-NM") : productPath.ToLower().Contains("tl") ? _etlPaths.CycleIdPath.Replace("{type}", "TL-NM") : productPath.ToLower().Contains("pf") ? _etlPaths.CycleIdPath.Replace("{type}", "PF-NM") :_etlPaths.CycleIdPath.Replace("{type}", "MG-NM"), cycleId, false);
                string em1tarstatus = TarProcess(Path.Combine(ematchFolder, "EXTRACT1"), cycleId, false);

                _ = await _microBatchService.UpdateBatchHistoryTable("generation", productFullName, processServer+":completed", cycleId);
            }
            catch (Exception ex)
            {

                string cycleIdFile = productPath.ToLower().Contains("asb") ? Path.Combine(_etlPaths.CycleIdPath.Replace("{type}", "ASB-NM"), "asb_1_cycle.id") : productPath.ToLower().Contains("tl") ? Path.Combine(_etlPaths.CycleIdPath.Replace("{type}", "TL-NM"), "tl_1_cycle.id") : productPath.ToLower().Contains("mg") ? Path.Combine(_etlPaths.CycleIdPath.Replace("{type}", "MG-NM"), "mg_1_cycle.id") : Path.Combine(_etlPaths.CycleIdPath.Replace("{type}", "PF-NM"), "pf_1_cycle.id");

                var cycleId = File.ReadLines(cycleIdFile).ElementAt(0);

                var data = _microBatchService.UpdateProgressTable("Fail", cycleId);

                int retry = 1;

                while (data == null && retry < _statementGeneration.DbRetryAttempts)
                {
                    Thread.Sleep(_statementGeneration.DbRetryWaitTime*1000);

                    Log.Information($"Retry for progress table generate db fail case {retry} id:"+cycleId);

                    data = _microBatchService.UpdateProgressTable("Fail", cycleId);

                    retry++;
                }

                _ = await _microBatchService.UpdateBatchHistoryTable("generation", productFullName, processServer + ":failed", cycleId);

                Log.Error(ex, "Error while generating.");

                File.WriteAllText(productPath + "/generate.error", "1");

                string path = Path.Combine(ematchFolder, "MATCH", new DirectoryInfo(productPath).Name);

                var freason = await Backup(path);

                Log.Error("Backup status for ematch failure backup: " + freason);

                string e2path = Path.Combine(ematchFolder, "EXTRACT2", new DirectoryInfo(productPath).Name);

                var e2reason = await Backup(e2path);

                Log.Error("Backup status for e2 folder failure backup: " + e2reason);

            }
            Log.Information("Statements generation ended."+productPath);
        }

        public async Task<bool> InvokeGenerateProcess(string folder, AlsInfo alsSettings)
        {
            string ematchextract2Folder = Path.Combine(folder.ToLower().Contains("asb") ? _etlPaths.EmatchFolder.Replace("{type}", "ASB-NM") : folder.ToLower().Contains("tl") ? _etlPaths.EmatchFolder.Replace("{type}", "TL-NM") : folder.ToLower().Contains("pf") ? _etlPaths.EmatchFolder.Replace("{type}", "PF-NM") :_etlPaths.EmatchFolder.Replace("{type}", "MG-NM"), "EXTRACT2");

            try
            {
                Log.Information("Generation started for " + folder);

                string cycleIdFile = folder.ToLower().Contains("asb") ? Path.Combine(_etlPaths.CycleIdPath.Replace("{type}", "ASB-NM"), "asb_1_cycle.id") : folder.ToLower().Contains("tl") ? Path.Combine(_etlPaths.CycleIdPath.Replace("{type}", "TL-NM"), "tl_1_cycle.id") : folder.ToLower().Contains("mg") ? Path.Combine(_etlPaths.CycleIdPath.Replace("{type}", "MG-NM"), "mg_1_cycle.id") : Path.Combine(_etlPaths.CycleIdPath.Replace("{type}", "PF-NM"), "pf_1_cycle.id");

                string statementDate = File.ReadLines(cycleIdFile.Replace("cycle.id", "stmt.date").Replace("cycle.id", "stmt.date")).ElementAt(0);

                var stmtdate = File.ReadLines(cycleIdFile).ElementAt(0).Split("-")[1];

                string[] inputFiles = Directory.GetFiles(folder);
                string sourceFileName = File.ReadAllText(cycleIdFile.Replace("cycle.id", "cycle.properties")).Split("|")[0];

                //Region for Archive
                foreach (var file in inputFiles)
                {
                    if (!file.Contains("_ars") || file.Contains("report") || file.Contains("status") || file.Contains("inprogess") || file.Contains("ready") || file.Contains("generate.inprogress"))
                        continue;

                    string curentTime = DateTime.Now.ToString("yyyyMMddHHmmssfff");

                    Log.Information("ARS initiated for :" + file);

                    string batchCount = Path.GetFileNameWithoutExtension(file).Split("_")[4];

                    string opsFileDirectory = Directory.GetParent(alsSettings.OpsFilePath).FullName;

                    string stmtType = new DirectoryInfo(folder).Name;

                    Log.Information("ARS generation started for file :" + file);

                    try
                    {
                        string inputPath = GetInputPathByStamtType(stmtType, alsSettings);

                        string outputPath = "";

                        try
                        {
                            outputPath = CreateOpsFile(inputPath, stmtType, file, opsFileDirectory, alsSettings.OpsFilePath, batchCount, "ARS", stmtdate, curentTime);

                            if (outputPath == "error")
                            {
                                Log.Information("OPS file creation failed");
                                return false;
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, $"Error while creating ops {stmtType}");
                            return false;
                        }

                        var doc1result = "";

                        try
                        {
                            var scriptData = File.ReadAllText(alsSettings.ShellScriptPaths.Doc1GenScript.Replace("{type}", folder.Split("/")[3]));

                            scriptData = scriptData.Replace("<deliveryType>", "ARS").Replace("<hipname>", "ARS").Replace("<opsname>", "ARS").Replace("<fileStmtType>", stmtType.Replace("-", "_")).Replace("<folderStmtType>", stmtType);

                            if (!Directory.Exists(Path.Combine(_etlPaths.ETLHomePath, "TEMP")))
                                Directory.CreateDirectory(Path.Combine(_etlPaths.ETLHomePath, "TEMP"));

                            string genScript = Path.Combine(_etlPaths.ETLHomePath, "TEMP") + "/GenScript" + stmtType + ".sh";

                            Log.Information("Genscript created for: ARS => " + genScript);

                            File.WriteAllText(genScript, scriptData);

                            doc1result = ProcessCommandLine(genScript, stmtType, file);
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, "Error while executing doc1gen");
                            return false;
                        }
                        string basefileName = ""; string extract2FilePath = "";

                        if (doc1result.Contains("successfully"))
                        {

                            //Get ematch input filenames 

                            var ematchInputFiles = Directory.GetFiles(outputPath, $"*.jrn");

                            if (ematchInputFiles.Length > 0)
                                basefileName = ematchInputFiles[0].Replace(".jrn", "");

                            extract2FilePath = ematchextract2Folder + $"/{stmtType}/BATCH_{batchCount}/{Path.GetFileName(basefileName)}";

                            Log.Information("Jrn File Path " + basefileName);

                            if (!Directory.Exists(ematchextract2Folder + $"/{stmtType}/BATCH_{batchCount}"))
                                Directory.CreateDirectory(ematchextract2Folder + $"/{stmtType}/BATCH_{batchCount}");

                            var data = await CreateFileFromJrn(basefileName + ".jrn", extract2FilePath, sourceFileName);

                            Log.Information($"Batch generation processed for {stmtType} : " + batchCount);
                        }
                        else
                        {
                            Log.Error("Error while calling Doc1Gen: " + doc1result);

                            return false;
                        }

                        Log.Information("Ematch process initiated.");

                        var extract1path = folder.ToLower().Contains("asb") ? Path.Combine(_etlPaths.EmatchFolder.Replace("{type}", "ASB-NM"), "EXTRACT1", "asb_1_extract1.txt") : folder.ToLower().Contains("tl") ? Path.Combine(_etlPaths.EmatchFolder.Replace("{type}", "TL-NM"), "EXTRACT1", "tl_1_extract1.txt") : folder.ToLower().Contains("pf") ? Path.Combine(_etlPaths.EmatchFolder.Replace("{type}", "PF-NM"), "EXTRACT1", "pf_1_extract1.txt") : Path.Combine(_etlPaths.EmatchFolder.Replace("{type}", "MG-NM"), "EXTRACT1", "mg_1_extract1.txt");

                        var ematchStatus = Ematch.EmatchProcess(extract1path, extract2FilePath+"_extract2.txt", Path.GetFileName(basefileName),"NM");

                        if (!ematchStatus)
                        {
                            Log.Error("eMatch Failed for ARS");
                            return false;
                        }

                        if (File.Exists(Path.Combine(Path.GetDirectoryName(extract2FilePath).Replace("EXTRACT2", "MATCH"), Path.GetFileName(basefileName)+"_datamatch.txt")))
                        {
                            var csvdumpStatus = await _microBatchService.LoadEmatchCSVFile(Path.Combine(Path.GetDirectoryName(extract2FilePath).Replace("EXTRACT2", "MATCH"), Path.GetFileName(basefileName)+"_datamatch.txt"), "ccss_ln_ematch_ars_report");

                            Log.Information("eMatch database upload status: "+csvdumpStatus);

                            if (!csvdumpStatus)
                                return false;
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Error in processing file: " + file);
                        return false;
                    }
                }

                //Region for Email Delivery 
                foreach (var file in inputFiles)
                {
                    if (!file.Contains("_eml") || file.Contains("report") || file.Contains("status") || file.Contains("inprogess") || file.Contains("ready") || file.Contains("generate.inprogress"))
                        continue;

                    Log.Information("EML initiated for :" + file);

                    string batchCount = Path.GetFileNameWithoutExtension(file).Split("_")[4];

                    string opsFileDirectory = Directory.GetParent(alsSettings.OpsFilePath).FullName;

                    string stmtType = new DirectoryInfo(folder).Name;

                    Log.Information("EML generation started for file :" + file);

                    try
                    {
                        string inputPath = GetInputPathByStamtType(stmtType, alsSettings);

                        string outputPath = "";

                        try
                        {
                            string curentTime = DateTime.Now.ToString("yyyyMMddHHmmssfff");

                            outputPath = CreateOpsFile(inputPath, stmtType, file, opsFileDirectory, alsSettings.OpsFilePath, batchCount, "EML", stmtdate, curentTime);
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, $"Error while creating ops {stmtType}");
                            return false;
                        }

                        var scriptData = File.ReadAllText(alsSettings.ShellScriptPaths.Doc1GenScript.Replace("{type}", folder.Split("/")[3]));

                        //For HTML

                        scriptData = scriptData.Replace("<deliveryType>", "EML").Replace("<hipname>", "EML_EHTML").Replace("<opsname>", "EML_EHTML").Replace("<fileStmtType>", stmtType.Replace("-", "_")).Replace("<folderStmtType>", stmtType);

                        if (!Directory.Exists(Path.Combine(_etlPaths.ETLHomePath, "TEMP")))
                            Directory.CreateDirectory(Path.Combine(_etlPaths.ETLHomePath, "TEMP"));

                        string genScript = Path.Combine(_etlPaths.ETLHomePath, "TEMP") + "/GenScript" + stmtType + ".sh";

                        Log.Information("Genscript created for: EML => " + genScript);

                        File.WriteAllText(genScript, scriptData);

                        var result = ProcessCommandLine(genScript, stmtType, file);

                        Log.Information("EML Doc1gen command execution Completed: " +stmtType);

                        //End HTML

                        scriptData = File.ReadAllText(alsSettings.ShellScriptPaths.Doc1GenScript.Replace("{type}", folder.Split("/")[3]));

                        scriptData = scriptData.Replace("<deliveryType>", "EML").Replace("<hipname>", "EML").Replace("<opsname>", "EML").Replace("<fileStmtType>", stmtType.Replace("-", "_")).Replace("<folderStmtType>", stmtType);

                        if (!Directory.Exists(Path.Combine(_etlPaths.ETLHomePath, "TEMP")))
                            Directory.CreateDirectory(Path.Combine(_etlPaths.ETLHomePath, "TEMP"));


                        genScript = Path.Combine(_etlPaths.ETLHomePath, "TEMP") + "/GenScript" + stmtType + ".sh";

                        Log.Information("Genscript created for: EML => " + genScript);

                        File.WriteAllText(genScript, scriptData);

                        result = ProcessCommandLine(genScript, stmtType, file);

                        string basefileName = "", extract2FilePath = "";

                        if (result.Contains("successfully"))
                        {

                            //Get ematch input filenames 

                            var ematchInputFiles = Directory.GetFiles(outputPath, $"*.dij");

                            if (ematchInputFiles.Length > 0)
                                basefileName = ematchInputFiles[0].Replace(".dij", "");

                            extract2FilePath = ematchextract2Folder + $"/{stmtType}/BATCH_{batchCount}/{Path.GetFileName(basefileName)}";

                            Log.Information("Jrn File Path " + basefileName);

                            Directory.CreateDirectory(ematchextract2Folder + $"/{stmtType}/BATCH_{batchCount}");

                            var data = await CreateFileFromJrn(basefileName + ".dij", extract2FilePath, sourceFileName);

                            Log.Information($"Batch generation processed for {stmtType} : " + batchCount);
                        }
                        else
                            return false;
                        Log.Information("Ematch process initiated.");

                        var extract1path = inputPath.ToLower().Contains("asb") ? Path.Combine(_etlPaths.EmatchFolder.Replace("{type}", "ASB-NM"), "EXTRACT1", "asb_1_extract1.txt") : inputPath.ToLower().Contains("tl") ? Path.Combine(_etlPaths.EmatchFolder.Replace("{type}", "TL-NM"), "EXTRACT1", "tl_1_extract1.txt") : inputPath.ToLower().Contains("pf") ? Path.Combine(_etlPaths.EmatchFolder.Replace("{type}", "PF-NM"), "EXTRACT1", "pf_1_extract1.txt") : Path.Combine(_etlPaths.EmatchFolder.Replace("{type}", "MG-NM"), "EXTRACT1", "mg_1_extract1.txt");

                        var ematchStatus = Ematch.EmatchProcess(extract1path, extract2FilePath+"_extract2.txt", Path.GetFileName(basefileName),"NM");

                        if (!ematchStatus)
                        {
                            Log.Error("eMatch Failed for EML");
                            return false;
                        }

                        if (ematchStatus && File.Exists(Path.Combine(Path.GetDirectoryName(extract2FilePath).Replace("EXTRACT2", "MATCH"), Path.GetFileName(basefileName)+"_datamatch.txt")))
                        {
                            var csvdumpStatus = await _microBatchService.LoadEmatchCSVFile(Path.Combine(Path.GetDirectoryName(extract2FilePath).Replace("EXTRACT2", "MATCH"), Path.GetFileName(basefileName)+"_datamatch.txt"), "ccss_ln_ematch_eml_report");

                            Log.Information("eMatch database upload status: "+csvdumpStatus);
                            if (!csvdumpStatus)
                                return false;
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Error in processing file: " + file);
                        return false;
                    }
                }

                //For Print
                foreach (var file in inputFiles)
                {
                    if (!file.Contains("_prt") || file.Contains("report") || file.Contains("status") || file.Contains("inprogess") || file.Contains("ready") || file.Contains("generate.inprogress"))
                        continue;

                    string curentTime = DateTime.Now.ToString("yyyyMMddHHmmssfff");

                    Log.Information("PRT initiated for :" + file);

                    string batchCount = Path.GetFileNameWithoutExtension(file).Split("_")[5];

                    string opsFileDirectory = Directory.GetParent(alsSettings.OpsFilePath).FullName;

                    string stmtType = new DirectoryInfo(folder).Name;

                    Log.Information("PRT generation started for file :" + file);

                    try
                    {
                        string inputPath = GetInputPathByStamtType(stmtType, alsSettings);

                        string outputPath = "";

                        bool prtl = false;

                        try
                        {
                            outputPath = CreateOpsFile(inputPath, stmtType, file, opsFileDirectory, alsSettings.OpsFilePath, batchCount, "PRT", stmtdate, curentTime);
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, $"Error while creating ops {stmtType}");

                            return false;
                        }

                        var scriptData = File.ReadAllText(alsSettings.ShellScriptPaths.Doc1GenScript.Replace("{type}", folder.Split("/")[3]));

                        if (file.ToLower().Contains("_l_"))
                        {
                            prtl = true;

                            scriptData = scriptData.Replace("<deliveryType>", "PRT").Replace("<hipname>", "PRT").Replace("<opsname>", "PRT_L").Replace("<fileStmtType>", stmtType.Replace("-", "_")).Replace("<folderStmtType>", stmtType);
                        }
                        else
                            scriptData = scriptData.Replace("<deliveryType>", "PRT").Replace("<hipname>", "PRT").Replace("<opsname>", "PRT_O").Replace("<fileStmtType>", stmtType.Replace("-", "_")).Replace("<folderStmtType>", stmtType);

                        if (!Directory.Exists(Path.Combine(_etlPaths.ETLHomePath, "TEMP")))
                            Directory.CreateDirectory(Path.Combine(_etlPaths.ETLHomePath, "TEMP"));

                        string genScript = Path.Combine(_etlPaths.ETLHomePath, "TEMP") + "/GenScript" + stmtType + ".sh";

                        Log.Information("Genscript created for: PRT => " + genScript);

                        File.WriteAllText(genScript, scriptData);

                        var result = ProcessCommandLine(genScript, stmtType, file);

                        string basefileName = "", extract2FilePath = "";

                        if (result.Contains("successfully"))
                        {
                            //Get ematch input filenames 

                            string[] ematchInputFiles;

                            if (prtl)
                                ematchInputFiles = Directory.GetFiles(outputPath, $"*_l_*.txt");
                            else
                                ematchInputFiles = Directory.GetFiles(outputPath, $"*_o_*.txt");

                            if (ematchInputFiles.Length > 0)
                                basefileName = ematchInputFiles[0].Replace(".txt", "");

                            extract2FilePath = ematchextract2Folder + $"/{stmtType}/BATCH_{batchCount}/{Path.GetFileName(basefileName)}";

                            Log.Information("Txt File Path " + basefileName);

                            Directory.CreateDirectory(ematchextract2Folder + $"/{stmtType}/BATCH_{batchCount}");

                            _ = await CreateFileFromGenerateTxt(basefileName + ".txt", extract2FilePath, sourceFileName);

                            Log.Information($"Batch generation processed for {stmtType} : " + batchCount);
                        }
                        else
                        {
                            Log.Error("Doc1 Generation failed for print");
                            return false;
                        }

                        Log.Information("Ematch process initiated.");

                        var extract1path = folder.ToLower().Contains("asb") ? Path.Combine(_etlPaths.EmatchFolder.Replace("{type}", "ASB-NM"), "EXTRACT1", "asb_1_extract1.txt") : folder.ToLower().Contains("tl") ? Path.Combine(_etlPaths.EmatchFolder.Replace("{type}", "TL-NM"), "EXTRACT1", "tl_1_extract1.txt") : folder.ToLower().Contains("pf") ? Path.Combine(_etlPaths.EmatchFolder.Replace("{type}", "PF-NM"), "EXTRACT1", "pf_1_extract1.txt") : Path.Combine(_etlPaths.EmatchFolder.Replace("{type}", "MG-NM"), "EXTRACT1", "mg_1_extract1.txt");

                        var ematchStatus = Ematch.EmatchProcess(extract1path, extract2FilePath+"_extract2.txt", Path.GetFileName(basefileName), "NM");

                        if (!ematchStatus)
                        {
                            Log.Error("eMatch Failed for PRT");
                            return false;
                        }

                        if (ematchStatus && File.Exists(Path.Combine(Path.GetDirectoryName(extract2FilePath).Replace("EXTRACT2", "MATCH"), Path.GetFileName(basefileName)+"_datamatch.txt")))
                        {
                            var csvdumpStatus = await _microBatchService.LoadEmatchCSVFile(Path.Combine(Path.GetDirectoryName(extract2FilePath).Replace("EXTRACT2", "MATCH"), Path.GetFileName(basefileName)+"_datamatch.txt"), "ccss_ln_ematch_prt_report");

                            Log.Information("eMatch database upload status: "+csvdumpStatus);

                            if (!csvdumpStatus)
                                return false;
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Error in processing file: " + file);

                        var ematchbaseFolder = folder.ToLower().Contains("asb") ? _etlPaths.EmatchFolder.Replace("{type}", "ASB-NM") : folder.ToLower().Contains("tl") ? _etlPaths.EmatchFolder.Replace("{type}", "TL-NM") : folder.ToLower().Contains("pf") ? _etlPaths.EmatchFolder.Replace("{type}", "PF-NM") : _etlPaths.EmatchFolder.Replace("{type}", "MG-NM");

                        string path = Path.Combine(ematchbaseFolder, "MATCH", new DirectoryInfo(folder).Name);

                        var freason = Backup(path);

                        Log.Error(path+ " Backup status for ematch failure backup: "+ freason);

                        string e2path = Path.Combine(ematchbaseFolder, "EXTRACT2", new DirectoryInfo(folder).Name);

                        var e2reason = Backup(e2path);

                        Log.Error(e2path+"  Backup status for e2 folder failure backup: " + e2reason);

                        return false;
                    }
                }

                //Ematch Match Folder
                var ematchFolder = folder.ToLower().Contains("asb") ? _etlPaths.EmatchFolder.Replace("{type}", "ASB-NM") : folder.ToLower().Contains("tl") ? _etlPaths.EmatchFolder.Replace("{type}", "TL-NM") : folder.ToLower().Contains("pf") ? _etlPaths.EmatchFolder.Replace("{type}", "PF-NM") : _etlPaths.EmatchFolder.Replace("{type}", "MG-NM");

                string sourcePath = Path.Combine(ematchFolder, "MATCH", new DirectoryInfo(folder).Name);
                _ = await Backup(sourcePath);

                //Ematch Extract2 Folder
                string extract2 = Path.Combine(ematchFolder, "EXTRACT2", new DirectoryInfo(folder).Name);
                _ = await Backup(extract2);

                return true;

            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error in Generate Process");

                return false;
            }

        }

        public string GetInputPathByStamtType(string stmtType, AlsInfo settingPath)
        {

            switch (stmtType)
            {
                case "ASB":
                    return settingPath.InputFilePath.ASB;
                case "TL-WR":
                    return settingPath.InputFilePath.TLWR;
                case "TL-WOR":
                    return settingPath.InputFilePath.TLWOR;
                case "TL-CC":
                    return settingPath.InputFilePath.TLCC;
                case "PF-VR":
                    return settingPath.InputFilePath.PFVR;
                case "PF-FR":
                    return settingPath.InputFilePath.PFFR;
                case "MG-WR":
                    return settingPath.InputFilePath.MGWR;
                case "MG-WOR":
                    return settingPath.InputFilePath.MGWOR;
                case "MG-EQT":
                    return settingPath.InputFilePath.MGEQT;
                case "MG-BBA":
                    return settingPath.InputFilePath.MGBBA;
                default:
                    return null;

            }
        }

        public static string CreateOpsFile(string inputFilePath, string stmtType, string inputfileName, string opsDirectory, string opsTemplateFile, string batchCount, string outPutType, string stmtdate, string currenttime)
        {
            try
            {
                Log.Information("OPS file templated Path : " + opsTemplateFile);

                var opsContent = File.ReadAllText(opsTemplateFile);

                string outputPath = inputFilePath.Replace("INPUT", "OUTPUT");

                outputPath = Path.Combine(outputPath, outPutType, "BATCH_" + batchCount);

                if (!Directory.Exists(outputPath))
                {
                    Log.Information("Batch Folder created for: "+outputPath);

                    Directory.CreateDirectory(outputPath);
                }

                Log.Information("Input file" + inputfileName +"  OutputPath "+outputPath);

                string fbankCode = "";

                if (outputPath.Split("/")[6].ToLower().Contains("asb"))
                    fbankCode = "con";
                else
                    fbankCode= outputPath.Split("/")[6].Split("-")[1].ToLower();

                Log.Information("FBankcode: "+fbankCode);

                string type = outputPath.Split("/")[3];

                opsContent = opsContent.Replace("<inputfileName>", inputfileName).Replace("<stmtType>", stmtType)
                    .Replace("<opsTemplatePath>", opsDirectory).Replace("<OutputPath>", outputPath).Replace("<ProductNameShort>", inputfileName.Split(@"/")[3])
                    .Replace("<FProductNameShort>", inputfileName.Split(@"/")[3].Split("-")[0].ToLower()).Replace("<FOutputType>", outPutType.ToLower()).Replace("<BatchNumber>", batchCount)
                    .Replace("<Stmtdate>", stmtdate).Replace("<datetimenow>", currenttime).Replace("<Fbankcode>", fbankCode.ToLower()).Replace("<ProductType>", stmtType.ToLower()).Replace("<type>", type).Replace("<lookupfilename>", outPutType);

                string fileName = "ln_1_" + inputfileName.Split(@"/")[3].Split("-")[0].ToLower()+"-" + fbankCode + "_" + outPutType.ToLower() + $"_{batchCount}_" + stmtdate + "_" + currenttime + ".afp";

                if (!File.Exists(opsTemplateFile.Replace("AlsTemplate.ops", $"/LOOKUPS/{stmtType.ToLower() + outPutType}.txt")))
                    File.CreateText(opsTemplateFile.Replace("AlsTemplate.ops", $"/LOOKUPS/{stmtType.ToLower() + outPutType}.txt"));

                File.WriteAllText(opsTemplateFile.Replace("AlsTemplate.ops", $"/LOOKUPS/{stmtType.ToLower() + outPutType}.txt"), "Output_FileName "+fileName);

                if (outPutType == "ARS")
                {
                    opsContent = opsContent.Replace("ROutput1", "Output1");
                    string arsOpsFile = opsDirectory + "/" + stmtType + "-" + outPutType + "/" +  stmtType.Replace("-", "_") + "_" + outPutType + ".ops";

                    Log.Information("ARS OPS file location: " + arsOpsFile);

                    File.WriteAllText(arsOpsFile, opsContent);
                }

                if (outPutType == "PRT" && inputfileName.Contains("_l"))
                {
                    string printFilename = Path.GetFileNameWithoutExtension(fileName);

                    Log.Information("PRT_L File name: " + fileName);

                    Log.Information("Lookup file information updated in Lookup_L");

                    opsContent = opsContent.Replace("DIJ", "Journal").Replace("ROutput1", "REPORT").Replace(".jrn", ".txt").Replace(printFilename, Path.GetFileNameWithoutExtension(fileName)).Replace("prt_", "prt_l_").Replace("_l_l_", "_l_").Replace(".pdf", ".afp").Replace("<lookupfilename>", "Lookup_L");

                    File.WriteAllText(opsTemplateFile.Replace("AlsTemplate.ops", $"/LOOKUPS/{stmtType.ToLower() +outPutType}.txt"), "Output_FileName "+Path.GetFileNameWithoutExtension(fileName).Replace("prt_", "prt_l_")+".afp");

                    Log.Information("PRT_L ops data:" + opsContent);

                    string arsOpsFile = opsDirectory + "/" + stmtType + "-" + outPutType + "/" + stmtType.Replace("-", "_") + "_" + outPutType + "_L.ops";
                    Log.Information("PRT_L OPS file location: " + arsOpsFile);
                    File.WriteAllText(arsOpsFile, opsContent);
                }

                if (outPutType == "PRT" && inputfileName.Contains("_o"))
                {

                    string printFilename = Path.GetFileNameWithoutExtension(fileName);

                    Log.Information("PRT_O File name: " + fileName);

                    Log.Information("Lookup file information updated in Lookup_O");

                    opsContent = opsContent.Replace("DIJ", "Journal").Replace("ROutput1=", "REPORT=").Replace(".jrn", ".txt").Replace(printFilename, Path.GetFileNameWithoutExtension(fileName)).Replace("prt_", "prt_o_").Replace("_o_o_", "_o_").Replace(".pdf", ".afp").Replace("<lookupfilename>", "Lookup_O");

                    File.WriteAllText(opsTemplateFile.Replace("AlsTemplate.ops", $"/LOOKUPS/{stmtType.ToLower() +outPutType}.txt"), "Output_FileName "+Path.GetFileNameWithoutExtension(fileName).Replace("prt_", "prt_o_")+".afp");

                    Log.Information("PRT_O :OPS data " + opsContent);

                    string arsOpsFile = opsDirectory + "/" + stmtType + "-" + outPutType + "/" + stmtType.Replace("-", "_") + "_" + outPutType + "_O.ops";
                    Log.Information("PRT_O OPS file location: " + arsOpsFile);
                    File.WriteAllText(arsOpsFile, opsContent);
                }

                if (outPutType == "EML")
                {
                    File.WriteAllText(opsTemplateFile.Replace("AlsTemplate.ops", $"/LOOKUPS/{stmtType.ToLower() +outPutType}.txt"), "Output_FileName "+fileName.Replace(".afp", ""));

                    Log.Information("EML File name replace: " + fileName);

                    //Lookup file updating
                    Log.Information("Lookup file information updated in Lookup_E");

                    //opsContent = opsContent.Replace("F45_", "").Replace("EXT_KEY", "EXT_KEY_IMAGE").Replace(fileName, fileName.Replace(".afp","")+"%3.pdf").Replace(".jrn", ".dij").Replace("ROutput1", "Output1");
                    opsContent = opsContent.Replace(fileName, fileName.Replace(".afp", "")+"%3.pdf").Replace(".jrn", ".dij").Replace("ROutput1", "Output1");

                    string arsOpsFile = opsDirectory + "/" + stmtType + "-" + outPutType + "/" + stmtType.Replace("-", "_") + "_" + outPutType + ".ops";

                    Log.Information("EML OPS file location: " + arsOpsFile);

                    File.WriteAllText(arsOpsFile, opsContent);

                    opsContent = File.ReadAllText(opsTemplateFile.Replace("AlsTemplate.ops", "Template_html.ops"));

                    opsContent = opsContent.Replace("<inputfileName>", inputfileName).Replace("<stmtType>", stmtType).Replace("<lookupfilename>", outPutType).Replace("<ProductType>", stmtType.ToLower())
                    .Replace("<opsTemplatePath>", opsDirectory).Replace("<OutputPath>", outputPath).Replace("<ProductNameShort>", inputfileName.Split(@"/")[3]).Replace("%3.html", fileName.Replace(".afp", "")+"%3.html");

                    string emlOpsFile = opsDirectory + "/" + stmtType + "-" + outPutType + "/" + stmtType.Replace("-", "_") + "_" + outPutType + "_EHTML.ops";

                    Log.Information("OPS file location: " + arsOpsFile);

                    File.WriteAllText(emlOpsFile, opsContent);
                }

                Log.Information("OPS file information updated.");

                return outputPath;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error while updating ops file info");

                return "error";
            }

        }
        public async Task<bool> ExecuteDoc1Batch(string batchPath, int? processWaitTime = null)
        {
            ProcessStartInfo info = new ProcessStartInfo("cmd.exe", "/c " + batchPath)
            {
                FileName = batchPath,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            using (Process process = Process.Start(info))
            {
                process.WaitForExit();
                process.BeginErrorReadLine();
                process.BeginOutputReadLine();
                string output = process.StandardOutput.ReadToEnd();
                process.Close();
                Log.Information("Got response from generate shell script: " + output);
            }

            return true;
        }

        public async Task<string> CreateFileFromJrn(string filename, string outputPath,string sourceFileName)
        {
            try
            {
                string stmtType = outputPath.Split("/")[6];

                string batchNumber = Path.GetFileName(filename).Split("_")[6];

                XDocument xml = XDocument.Load(filename);

                IEnumerable<XElement> xDoc = XDocument.Parse(xml.ToString()).Descendants("document");

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
                               let statementDate = (item.Elements("DDSDocValue")
                                   .Where(i => (string)i.Attribute("name") == "Statement_Date").Select(i => i.Value)).FirstOrDefault()
                               let deliverymethod = (item.Elements("DDSDocValue")
                                   .Where(i => (string)i.Attribute("name") == "Delivery_Method").Select(i => i.Value)).FirstOrDefault()
                               let dob = (item.Elements("DDSDocValue")
                                   .Where(i => (string)i.Attribute("name") == "DOB").Select(i => i.Value)).FirstOrDefault()
                               let idnumber = (item.Elements("DDSDocValue")
                                   .Where(i => (string)i.Attribute("name") == "Id_Number").Select(i => i.Value)).FirstOrDefault()
                               let productname = (item.Elements("DDSDocValue")
                                    .Where(i => (string)i.Attribute("name") == "Product_Name").Select(i => i.Value)).FirstOrDefault()
                               let producttype = (item.Elements("DDSDocValue")
                                    .Where(i => (string)i.Attribute("name") == "Product_Type").Select(i => i.Value)).FirstOrDefault()
                               let productcode = (item.Elements("DDSDocValue")
                               .Where(i => (string)i.Attribute("name") == "Product_Code").Select(i => i.Value)).FirstOrDefault()
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

                               let divisionCode = (item.Elements("DDSDocValue")
                                  .Where(i => (string)i.Attribute("name") == "Division_Code").Select(i => i.Value)).FirstOrDefault()
                               let journalFileName = (item.Elements("DDSDocValue")
                               .Where(i => (string)i.Attribute("name") == "File_Name").Select(i => i.Value)).FirstOrDefault()

                               let noofpages = (item.Elements("DDSDocValue")
                                .Where(i => (string)i.Attribute("name") == "No_Of_Pages").Select(i => i.Value)).FirstOrDefault()
                               let foreignaddressindicator = (item.Elements("DDSDocValue")
                                .Where(i => (string)i.Attribute("name") == "Foreign_Address_Indicator").Select(i => i.Value)).FirstOrDefault()

                               let loantype = (item.Elements("DDSDocValue")
                               .Where(i => (string)i.Attribute("name") == "Loan_Type").Select(i => i.Value)).FirstOrDefault()
                               let loanamount = (item.Elements("DDSDocValue")
                              .Where(i => (string)i.Attribute("name") == "Loan_Amount").Select(i => i.Value)).FirstOrDefault()
                               let branchcode = (item.Elements("DDSDocValue")
                               .Where(i => (string)i.Attribute("name") == "Branch_Code").Select(i => i.Value)).FirstOrDefault()
                               let branchname = (item.Elements("DDSDocValue")
                              .Where(i => (string)i.Attribute("name") == "Branch_Name").Select(i => i.Value)).FirstOrDefault()

                               let accNo = (item.Descendants("AccNo").FirstOrDefault().Value)
                               let vendorId = (item.Descendants("VendorId").FirstOrDefault().Value)
                               let docTypeId = (item.Descendants("DocTypeId").FirstOrDefault().Value)
                               let docID = (item.Attribute("docID").Value)

                               select new GenerateLog
                               {
                                   Id = accNo,
                                   CycleID = cycleid,
                                   Email = email,
                                   Name_1 = name1,
                                   Address1 = addr1,
                                   Address2 = addr2,
                                   PostCode = postcode,
                                   State = state,
                                   CISNumber = cisnumber,
                                   AccountNumber = accountnumber,
                                   StatementDate = DateTime.ParseExact(statementDate, "MM/dd/yyyy", null),
                                   DeliveryMethod = deliverymethod,
                                   DOB = dob,
                                   IdNumber = idnumber,
                                   ProductName = productname,
                                   ProductType = producttype,
                                   BankCode = bankcode,
                                   Staff = staff,
                                   Division = division,
                                   DivisionCode = divisionCode,
                                   Premier = premier,
                                   Entity = entity,
                                   FileName = journalFileName,
                                   NoOfPages = noofpages,
                                   ForeignAddressIndicator = foreignaddressindicator,
                                   TotalPages = noofpages,
                                   LoanAmount=loanamount,
                                   LoanType=loantype,
                                   BranchCode=branchcode,
                                   BranchName=branchname,
                                   ProductCode=productcode,
                                   //DocID = docID,
                                   BatchType = "NM",
                                   ReportType = "Generation",
                                   Status = "SUCCESS",
                                   CreatedDate = DateTime.Now,
                                   UpdatedDate = DateTime.Now

                               }).ToList();

                StringBuilder sbRtn = new StringBuilder();

                sbRtn.AppendLine("Product Name|Product Type|Statement Date|Delivery Method|FileName|GUID|CycleID|Name1|Address 1|Address 2|PostCode|State|CIS Number|Account Number|DOB|Email|Loan Amount");

                foreach (var item in xmldata)
                {
                    var listResults = string.Format("{0}|{1}|{2}|{3}|{4}|{5}|{6}|{7}|{8}|{9}|{10}|{11}|{12}|{13}|{14}|{15}|{16}",
                                                         item.ProductName,
                                                         item.ProductType,
                                                         item.StatementDate.ToString("yyyy-MM-dd"),
                                                         item.DeliveryMethod,
                                                         sourceFileName,
                                                         item.Id,
                                                         item.CycleID,
                                                         item.Name_1,
                                                         item.Address1,
                                                         item.Address2,
                                                         item.PostCode,
                                                         item.State,
                                                         item.CISNumber,
                                                         item.AccountNumber,
                                                         item.DOB,
                                                         item.Email,
                                                         item.LoanAmount
                                                         );

                    try
                    {
                        if (!(filename.Contains("ARS") && (item.DeliveryMethod == "0" || item.DeliveryMethod == "6")))
                        {
                            var status = await _microBatchService.InsertCsvRecordToDatabase(item);

                            if(status==null)
                                return "Error";
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Error in database insert");
                        return "Error";
                    }
                    sbRtn.AppendLine(listResults);
                }

                string path = outputPath + "_extract2.txt";

                File.AppendAllText(path, sbRtn.ToString());

                Log.Information("JRN file data written to " + path);

                return "Success";
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error in txt file creation");

                return "Error";
            }
        }

        public async Task<string> CreateFileFromGenerateTxt(string filename, string outputPath,string sourceFileName)
        {
            try
            {
                Log.Information("Txt insertion initiated for file:" + filename);

                StringBuilder sbRtn = new StringBuilder();

                sbRtn.AppendLine("Product Name|Product Type|Statement Date|Delivery Method|FileName|GUID|CycleID|Name1|Address 1|Address 2|PostCode|State|CIS Number|Account Number|DOB|Email|Loan Amount");

                string stmtType = outputPath.Split("/")[6];

                using (StreamReader sr = new StreamReader(filename, Encoding.UTF8))
                {
                    string line = sr.ReadLine();

                    //skip line
                    line = sr.ReadLine();

                    while (line != null)
                    {
                        string[] parts = line.Split("|");
                        var record = new GenerateLog
                        {
                            Id = parts[0],
                            CycleID = parts[1],
                            Email = parts[2].Trim(),
                            Name_1 = parts[3].Trim(),
                            Address1 = parts[4].Trim(),
                            Address2 = parts[5].Trim(),
                            PostCode = parts[6].Trim(),
                            State = parts[7].Trim(),
                            CISNumber = parts[8].Trim(),
                            AccountNumber = parts[9].Trim(),
                            StatementDate = DateTime.ParseExact(parts[10], "MM/dd/yyyy", null),
                            DeliveryMethod = parts[11],
                            DOB = parts[12].Trim(),
                            IdNumber = parts[13],
                            ProductCode = parts[14].Trim(),
                            ProductName = parts[15],
                            ProductType = parts[16],
                            LoanType = parts[17],
                            BankCode = parts[18],
                            BranchCode = parts[19],
                            BranchName = parts[20],
                            DocumentType = parts[21],
                            Staff = parts[22],
                            Division = parts[23],
                            DivisionCode = parts[24],
                            Premier = parts[25],
                            Entity = parts[26],
                            FileName = parts[27],
                            LoanAmount = parts[28],
                            NoOfPages = parts[29],
                            ForeignAddressIndicator = parts[30],
                            TotalPages = parts[31],
                            BatchType = "NM",
                            ReportType = "Generation",
                            Status = "SUCCESS",
                            CreatedDate = DateTime.Now,
                            UpdatedDate = DateTime.Now
                        };

                        try
                        {
                            //Log.Information("PRT Generation insertion: " + record.Id);

                            var status = await _microBatchService.InsertCsvRecordToDatabase(record);

                            int retry = 1;

                            while (status == null && retry < _statementGeneration.DbRetryAttempts)
                            {
                                Thread.Sleep(_statementGeneration.DbRetryWaitTime * 1000);

                                Log.Information("Retry for for db fail case id:" + record.Id);

                                status = await _microBatchService.InsertCsvRecordToDatabase(record);

                                retry++;
                            }

                            if (status == null)
                                return "Error";
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, "Error in database insert PRT generate");
                        }

                        var listResults = string.Format("{0}|{1}|{2}|{3}|{4}|{5}|{6}|{7}|{8}|{9}|{10}|{11}|{12}|{13}|{14}|{15}|{16}",
                                                         record.ProductName,
                                                         record.ProductType,
                                                         record.StatementDate.ToString("yyyy-MM-dd"),
                                                         record.DeliveryMethod,
                                                         sourceFileName,
                                                         record.Id,
                                                         record.CycleID,
                                                         record.Name_1,
                                                         record.Address1,
                                                         record.Address2,
                                                         record.PostCode,
                                                         record.State,
                                                         record.CISNumber,
                                                         record.AccountNumber,
                                                         record.DOB,
                                                         record.Email,
                                                         record.LoanAmount);

                        sbRtn.AppendLine(listResults);

                        line = sr.ReadLine();
                    }
                }

                string path = outputPath + "_extract2.txt";

                File.AppendAllText(path, sbRtn.ToString());

                Log.Information("PRT txt Generated file data written to " + path);

                return "Success";
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error while prt file database insertion");
                return "Error";
            }
        }

        private static string ProcessCommandLine(string batchFilePath, string stmtType, string fileName = null)
        {
            if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Linux))
                return ProcessCommandlineOnLinux(batchFilePath, stmtType, fileName);
            else
                return ProcessCommandlineOnWinx(batchFilePath);
        }

        private static string ProcessCommandlineOnWinx(string batchFilePath)
        {
            var info = new ProcessStartInfo("cmd.exe", "/c" + batchFilePath)
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,

                FileName = batchFilePath,
                //Arguments = batchPath,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            using Process process = Process.Start(info);

            try
            {
                //process.WaitForExit(3000); //wait for max 5 min
                process.WaitForExit();
                string output = process.StandardOutput.ReadToEnd();
                process.Close();

                return output;
            }
            catch (Exception ex)
            {
                Log.Error(ex, ex.Message);
                return "Error";
            }
        }

        private static string ProcessCommandlineOnLinux(string batchFilePath, string stmtType, string fileName = null)
        {
            var processInfo = new ProcessStartInfo
            {
                UseShellExecute = false,
                FileName = "sh",
                Arguments = batchFilePath,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };

            using var process = Process.Start(processInfo);

            try
            {
                string output = process.StandardOutput.ReadToEnd();

                while (!process.HasExited)
                {
                    //Thread.Sleep(4000);
                    //Log.Information("Generation in process");
                }

                output = output +"\n"+ process.StandardOutput.ReadToEnd();

                if (stmtType != "")
                    Log.Information("Execution started for "+stmtType+ " For the file: "+fileName+Environment.NewLine+batchFilePath + " Output from shell script is: " + output + Environment.NewLine+"Shell script completed for: "+stmtType+ " For the file: "+fileName);
                else
                    Log.Information(batchFilePath + " Output from shell script is: " + output+Environment.NewLine+fileName);

                process.Close();

                return output;
            }
            catch (Exception ex)
            {
                Log.Error(ex, ex.Message);

                return "Error for "+stmtType+" File Name"+fileName;
            }
        }

        public async Task<bool> Backup(string sourcePath)
        {
            try
            {
                Log.Information($"BackUp Process initiated in Ematch for {sourcePath}");

                var batchDirectorys = Directory.GetDirectories(sourcePath, "BATCH_*");

                string cycleIdFile = sourcePath.ToLower().Contains("asb") ? Path.Combine(_etlPaths.CycleIdPath.Replace("{type}", "ASB-NM"), "asb_1_cycle.id") : sourcePath.ToLower().Contains("tl") ? Path.Combine(_etlPaths.CycleIdPath.Replace("{type}", "TL-NM"), "tl_1_cycle.id") : sourcePath.ToLower().Contains("mg") ? Path.Combine(_etlPaths.CycleIdPath.Replace("{type}", "MG-NM"), "mg_1_cycle.id") : Path.Combine(_etlPaths.CycleIdPath.Replace("{type}", "PF-NM"), "pf_1_cycle.id");

                var cycleId = File.ReadLines(cycleIdFile).ElementAt(0);

                string backUpFolder = Path.Combine(sourcePath, "BACKUP", cycleId);

                if (batchDirectorys.Count() == 0)
                {
                    Log.Information("Empty Folder: "+sourcePath);

                    processDirectory(sourcePath);

                    return true;

                }

                Directory.CreateDirectory(backUpFolder);

                foreach (var item in batchDirectorys)
                {
                    var files = Directory.GetFiles(item);

                    string batchFolder = Path.Combine(backUpFolder, new DirectoryInfo(item).Name);

                    Log.Information("Backup folder path: " + batchFolder);

                    Directory.CreateDirectory(batchFolder);

                    foreach (var file in files)
                    {
                        string backup = Path.Combine(batchFolder, Path.GetFileName(file));

                        Log.Information("backup file Path: " + backup);

                        File.Move(file, backup);
                    }
                    Directory.Delete(item);
                }

                Log.Information("Zip process initiated for " + backUpFolder);

                string backupscript = Path.Combine(_etlPaths.ETLHomePath, "TEMP", "gzip" + DateTime.Now.ToString("ddMMyyyyHHmmssfff") + ".sh");

                try
                {
                    File.WriteAllText(backupscript, $"cd {Path.Combine(sourcePath, "BACKUP")}\ntar -zcvf {cycleId}.tar.gz {cycleId}\nrm -r {cycleId}");

                    string process = ProcessCommandLine(backupscript, "");

                    Log.Information(backUpFolder+" GZip process status:" + process);

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
            foreach (var directory in Directory.GetDirectories(startLocation))
            {
                processDirectory(directory);
                if (Directory.GetFiles(directory).Length == 0 &&
                    Directory.GetDirectories(directory).Length == 0)
                {
                    Directory.Delete(directory, false);
                }
            }
        }

        public bool GetServerAvailability(string productPath)
        {
            try
            {
                var addresslist = Dns.GetHostEntry(Dns.GetHostName()).AddressList.Select(x => x.ToString()).ToList();
                //Log.Information("Product Path:"+productPath+" product path: "+_haConfig.Products);
                var serverPriority = _haConfig.ProductSettings[productPath.Split("/")[2]].Servers.Split(',');

                var sgServers = JsonConvert.SerializeObject(_haConfig.SG);

                var servers = JsonConvert.DeserializeObject<Dictionary<string, string>>(sgServers);

                foreach (var server in serverPriority)
                {
                    Ping pinger = new Ping();

                    PingReply reply = pinger.Send(servers[server]);

                    var pingable = reply.Status == IPStatus.Success;

                    if (pingable)
                    {
                        if (addresslist.Contains(servers[server]))
                        {
                            Log.Information($@"SG Process initiated on: {servers[server]}.");

                            processServer = server;

                            return true;
                        }

                        return false;
                    }
                    else
                    {
                        Log.Information($@" {servers[server]} is not reachable.");
                        continue;
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error while checking HA");
                return false;
            }
        }

        private string TarProcess(string rootpath, string cycleId, bool filescheck)
        {
            Log.Information("Tar process initiated for " + cycleId);

            string backupscript = Path.Combine(_etlPaths.ETLHomePath, "TEMP", "gzip" + DateTime.Now.ToString("ddMMyyyyHHmmssfff") + ".sh");

            try
            {
                string[] filesList = new string[] { };

                if (filescheck)
                    filesList = Directory.GetFiles(rootpath, "*.txt");
                else
                    filesList = Directory.GetFiles(rootpath);

                foreach (var file in filesList)
                {
                    string destinationDirectory = Path.Combine(rootpath, "BACKUP", cycleId);

                    if (!Directory.Exists(destinationDirectory))
                        Directory.CreateDirectory(destinationDirectory);

                    string destinationFile = @$"{destinationDirectory}/{Path.GetFileName(file)}";

                    try
                    {
                        if (filescheck)
                            File.Move(file, destinationFile, true);
                        else
                            File.Copy(file, destinationFile, true);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Error while moving file.");
                    }
                }

                File.WriteAllText(backupscript, $"cd {Path.Combine(rootpath, "BACKUP")}\ntar -zcvf {cycleId}.tar.gz {cycleId}\nrm -r {cycleId}");

                string process = ProcessCommandLine(backupscript, "");

                Log.Information(cycleId+" GZip process status:" + process);

                File.Delete(backupscript);

                return "success";
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error while making gzip script");
                return "fail";
            }
        }
    }
}
