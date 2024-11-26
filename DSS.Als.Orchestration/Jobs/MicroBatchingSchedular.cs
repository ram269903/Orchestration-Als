using Common.MicroBatch.Repository;
using Microsoft.Extensions.Options;
using Quartz;
using Serilog;
using System.Net.NetworkInformation;
using System.Net;
using System.Text;
using DSS.Als.Orchestration.CommonProcess;
using DSS.Als.Orchestration.Models;
using Newtonsoft.Json;

namespace RHB.Als.Orchestration.Jobs
{
    public class MicroBatchingSchedular : IJob
    {
        private readonly IOrchestrationService _orchestrationService;
        private readonly FilePaths _filePaths;
        private readonly EtlPaths _etlPaths;
        private readonly StatementGeneration _statementGeneration;
        private readonly HAConfig _haConfig;
        private string processServer = "";

        public MicroBatchingSchedular(IOptions<FilePaths> filePaths, IOrchestrationService orchestrationService, IOptions<HAConfig> haConfig, IOptions<EtlPaths> etlPaths, IOptions<StatementGeneration> statmentGeneration)
        {
            _filePaths = filePaths.Value;
            _orchestrationService = orchestrationService;
            _statementGeneration = statmentGeneration.Value;
            _etlPaths = etlPaths.Value;
            _haConfig = haConfig.Value;

        }

        public async Task Execute(IJobExecutionContext context)
        {
            try
            {
                //Log.Information("Micro Batch Scheduler start's at " + DateTime.Now);

                string[] stmtList = Directory.GetDirectories(_statementGeneration.AlsInfo.InputFilePath.RootPath.Replace("{type}","ASB-NM"));

                foreach (var item in stmtList)
                {
                    string[] readyFile = Directory.GetFiles(item, "ready");

                    if (readyFile.Length > 0)
                    {
                        var serverAvailable = GetServerAvailability(item);

                        if (serverAvailable)
                        {
                            processServer = processServer.Trim() == "Server1" ? "sg01" : processServer.Trim() == "Server2" ? "sg02" : processServer.Trim() == "Server3" ? "sg03" : processServer;
                            MicroBatchWithRecon(item);
                        }
                    }
                }

                stmtList = Directory.GetDirectories(_statementGeneration.AlsInfo.InputFilePath.RootPath.Replace("{type}", "TL-NM"));

                foreach (var item in stmtList)
                {
                    string[] readyFile = Directory.GetFiles(item, "ready");

                    if (readyFile.Length > 0)
                    {
                        var serverAvailable = GetServerAvailability(item);

                        if (serverAvailable)
                        {
                            processServer = processServer.Trim() == "Server1" ? "sg01" : processServer.Trim() == "Server2" ? "sg02" : processServer.Trim() == "Server3" ? "sg03" : processServer;
                            MicroBatchWithRecon(item);
                        }
                    }
                }

                stmtList = Directory.GetDirectories(_statementGeneration.AlsInfo.InputFilePath.RootPath.Replace("{type}", "MG-NM"));

                foreach (var item in stmtList)
                {
                    string[] readyFile = Directory.GetFiles(item, "ready");

                    if (readyFile.Length > 0)
                    {
                        var serverAvailable = GetServerAvailability(item);

                        if (serverAvailable)
                        {
                            processServer = processServer.Trim() == "Server1" ? "sg01" : processServer.Trim() == "Server2" ? "sg02" : processServer.Trim() == "Server3" ? "sg03" : processServer;
                            MicroBatchWithRecon(item);
                        }
                    }
                }

                stmtList = Directory.GetDirectories(_statementGeneration.AlsInfo.InputFilePath.RootPath.Replace("{type}", "PF-NM"));

                foreach (var item in stmtList)
                {
                    string[] readyFile = Directory.GetFiles(item, "ready");

                    if (readyFile.Length > 0)
                    {
                        var serverAvailable = GetServerAvailability(item);

                        if (serverAvailable)
                        {
                            processServer = processServer.Trim() == "Server1" ? "sg01" : processServer.Trim() == "Server2" ? "sg02" : processServer.Trim() == "Server3" ? "sg03" : processServer;
                            MicroBatchWithRecon(item);
                        }
                    }
                }


            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error in Scheduler job. ");
            }

            await Task.CompletedTask;
        }


        public void MicroBatchWithRecon(string InputProductPath)
        {
            Log.Information("MicroBatch initiated for "+InputProductPath);

            
            string stmtType = new DirectoryInfo(InputProductPath).Name;

            string productFullName = Helper.GetFullNames(stmtType);

            try
            {
                string destinationDirectory;
                string tarId="";

                File.Move(InputProductPath + "/ready", InputProductPath + "/microbatch.inprogress", true);

                string[] filesList = Directory.GetFiles(InputProductPath, "*.txt");

                string filestatus = "";

                string cycleIdFile = InputProductPath.ToLower().Contains("asb") ? Path.Combine(_etlPaths.CycleIdPath.Replace("{type}", "ASB-NM"), "asb_1_cycle.id") : InputProductPath.ToLower().Contains("tl") ? Path.Combine(_etlPaths.CycleIdPath.Replace("{type}", "TL-NM"), "tl_1_cycle.id") : InputProductPath.ToLower().Contains("mg") ? Path.Combine(_etlPaths.CycleIdPath.Replace("{type}", "MG-NM"), "mg_1_cycle.id") : Path.Combine(_etlPaths.CycleIdPath.Replace("{type}", "PF-NM"), "pf_1_cycle.id");

                var cycleId = File.ReadLines(cycleIdFile).ElementAt(0);

                _ = _orchestrationService.UpdateBatchHistoryTable("generation", productFullName, processServer+":in-progress", cycleId);

                foreach (string file in filesList)
                {
                    if (Path.GetFileNameWithoutExtension(file) == "microbatch.inprogress")
                        continue;

                    if (File.Exists(file))
                    {
                        Log.Information($"Trying to split for the file [{file}].");

                        string rootFolderPath = new FileInfo(file).Directory.FullName;

                        tarId = cycleId;

                        destinationDirectory = Path.Combine(rootFolderPath, "BACKUP", cycleId);

                        if (!Directory.Exists(destinationDirectory))

                            Directory.CreateDirectory(destinationDirectory);

                        string destinationFile = @$"{destinationDirectory}/{Path.GetFileName(file)}";

                        try
                        {
                            File.Move(file, destinationFile, true);
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, "Error while moving file.");
                        }

                        //For how many customers has to sperate in each file
                        int batchSize = _filePaths.BatchSize;

                        var list = new List<string>();

                        var customerData = new List<string>();

                        var liness = Filereader(destinationFile);

                        int subFilesCount = 0;

                        int recordCount = 0;

                        int internalFileCount = 1;

                        string mainFile = Path.GetFileNameWithoutExtension(file);

                        for (int j = 0; j < liness.Count(); j++)
                        {
                            if (liness.ElementAt(j).StartsWith("01|"))
                            {
                                list.AddRange(customerData);
                                customerData = new List<string>();
                                customerData.Add(liness[j]);
                                recordCount++;
                            }
                            else
                                customerData.Add(liness[j]);

                            if (batchSize+1 == recordCount)
                            {
                                var destinationFilePath = rootFolderPath + "/" + mainFile + "_" + internalFileCount.ToString("D5") + ".txt";

                                File.WriteAllLines(destinationFilePath, list);

                                subFilesCount = subFilesCount + list.Count();

                                list = new List<string>();

                                internalFileCount++;

                                recordCount = 1;
                            }

                        }

                        if(customerData.Count >0)
                            list.AddRange(customerData);

                        if (list.Count > 0)
                        {
                            var destinationFilePath = rootFolderPath + "/" + mainFile + "_" + internalFileCount.ToString("D5") + ".txt";

                            File.WriteAllLines(destinationFilePath, list);

                            subFilesCount = subFilesCount + list.Count();

                            list = new List<string>();
                        }

                        if (subFilesCount == liness.Count)
                        {
                            filestatus = "0";

                            File.WriteAllText(rootFolderPath + "/" + mainFile + "_status.txt", "0");

                            File.WriteAllText(rootFolderPath + "/" + mainFile + "_report.txt", "No of lines: " + liness.Count);
                        }
                        else
                        {
                            filestatus = "1";
                            File.WriteAllText(rootFolderPath + "/" + mainFile + "_status.txt", "1");

                            File.WriteAllText(rootFolderPath + "/" + mainFile + "_report.txt", "No of lines: " + liness.Count);
                        }
                    }
                }

                Log.Information($"{InputProductPath} Micro batch process completed");

                File.WriteAllText(InputProductPath + "/microbatch.ready", filestatus);
            }
            catch (Exception ex)
            {
                //_ = MailProcess.UrlHit("");

                Log.Error(ex, "Error while doing Micro Batch.");

                File.WriteAllText(InputProductPath + "/microbatch.error", "1");
                File.WriteAllText(InputProductPath + "/generate.error", "1");

                string cycleIdFile = InputProductPath.ToLower().Contains("asb") ? Path.Combine(_etlPaths.CycleIdPath.Replace("{type}", "ASB-NM"), "asb_1_cycle.id") : InputProductPath.ToLower().Contains("tl") ? Path.Combine(_etlPaths.CycleIdPath.Replace("{type}", "TL-NM"), "tl_1_cycle.id") : InputProductPath.ToLower().Contains("mg") ? Path.Combine(_etlPaths.CycleIdPath.Replace("{type}", "MG-NM"), "mg_1_cycle.id") : Path.Combine(_etlPaths.CycleIdPath.Replace("{type}", "PF-NM"), "pf_1_cycle.id");

                var cycleId = File.ReadLines(cycleIdFile).ElementAt(0);

                var data =  _orchestrationService.UpdateProgressTable("Fail", cycleId);

                int retry = 1;

                while (data == null && retry < _statementGeneration.DbRetryAttempts)
                {
                    Thread.Sleep(_statementGeneration.DbRetryWaitTime*1000);

                    Log.Information($"Retry for progress table microbatch db fail case {retry} id:"+cycleId);

                    data = _orchestrationService.UpdateProgressTable("Fail", cycleId);

                    retry++;
                }

                _ = _orchestrationService.UpdateBatchHistoryTable("generation", productFullName, processServer+":failed", cycleId);
            }
        }

        public List<string> Filereader(string data)
        {
            var list = new List<string>();

            using (var fileStream = new FileStream(data, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (var streamReader = new StreamReader(fileStream, Encoding.UTF8))
                {
                    string line;

                    while ((line = streamReader.ReadLine()) != null)
                    {
                        list.Add(line);
                    }
                    streamReader.Close();
                }
                fileStream.Close();
            }
            return list;
        }

        public bool GetServerAvailability(string productPath)
        {
            try
            {
                var addresslist = Dns.GetHostEntry(Dns.GetHostName()).AddressList.Select(x => x.ToString()).ToList();
                Log.Information("Product Path:"+productPath+" product path: "+_haConfig.Products);
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
    }
}
