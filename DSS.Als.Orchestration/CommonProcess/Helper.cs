using Common.DataAccess.MsSql.QueryBuilder;
using Common.DataAccess.RDBMS;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace DSS.Als.Orchestration.CommonProcess
{
    public class Helper
    {
        
        public static string GetFullNames(string shortName) {
            try
            {
                //Log.Information("Short code:"+shortName+" ::  "+folderAndFullNameMap[shortName]);

                //return folderAndFullNameMap[shortName];

                switch (shortName)
                {
                    case "ASB":
                        return "ASB Loan/Financing";
                    case "TL-WR":
                        return "Term Loan With Redraw";
                    case "TL-WOR":
                        return "Term Loan Without Redraw";
                    case "TL-CC":
                        return "Term Loan - Corporate Commercial";
                    case "PF-VR":
                        return "Personal Financing - Variable Rate";
                    case "PF-FR":
                        return "Personal Financing - Flat Rate";
                    case "MG-WR":
                        return "MG With Redraw";
                    case "MG-WOR":
                        return "MG Without Redraw";
                    case "MG-EQT":
                        return "MG Islamic Equity";
                    case "MG-BBA":
                        return "MG Islamic BBA";
                    default:
                        return null;

                }
            }
            catch(Exception ex)
            {
                Log.Error(ex,"Error while getting product full name");
                return null;
            }
        }

        public static string ProcessCommandLine(string batchFilePath)
        {
            if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Linux))
                return ProcessCommandlineOnLinux(batchFilePath);
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

        private static string ProcessCommandlineOnLinux(string batchFilePath)
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
                //process.WaitForExit(5000);

                string output = process.StandardOutput.ReadToEnd();

                while (!process.HasExited)
                {
                    //Thread.Sleep(4000);
                    //Log.Information("Generation in process");
                }

                //process.WaitForExit();

                output = output +"\n"+ process.StandardOutput.ReadToEnd();

                //Log.Information("Error stream at script is "+process.StandardError.ReadToEnd());

                Log.Information(batchFilePath + " Output from shell script is: " + output);
                process.Close();

                return output;
            }
            catch (Exception ex)
            {
                Log.Error(ex, ex.Message);

                return "Error";
            }
        }

        public static bool HardCopySummaryReport(string source, string outputfile)
        {
            try
            {
                Log.Information("Hardcopy summary report initiated for :"+source);

                List<string[]> lst = new List<string[]>();

                using (var stream = File.OpenRead(source))
                {
                    using (var streamReader = new StreamReader(stream))
                    {
                       // var line = streamReader.ReadLine();
                        while (!streamReader.EndOfStream)
                        {
                           var line = streamReader.ReadLine();
                            string[] rows = Regex.Split(line, "\\|(?=(?:[^\"]*\"[^\"]*\")*[^\']*[^\"]*$)");
                            lst.Add(rows);
                        }
                    }
                }

                var items = lst.Select(x => x[27]).Distinct().ToList();

                StringBuilder str = new StringBuilder();

                str.AppendLine($"00|LN|{DateTime.Now.ToString("yyyyMMddHHmmss")}");

                foreach (var item in items)
                {
                    str.AppendLine($"01|{item}|{lst.Where(x => x[27] == item).Count()}|{lst.Where(x => x[27] == item).Sum(x => x[29].AsInt())}");
                }

                str.AppendLine($"99|{items.Count()}|{lst.Count()}|{lst.Sum(x => x[29].AsInt())}");

                File.WriteAllText(outputfile, str.ToString());

                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex,"Error in Hardcopy summary report");

                return false;
            }
        }
    }
}
