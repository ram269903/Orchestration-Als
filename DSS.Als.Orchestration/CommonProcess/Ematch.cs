using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DSS.Als.Orchestration.CommonProcess
{
    public class Ematch
    {
        public static bool EmatchProcess(string extract1Name, string extract2Name, string fileName, string batchType)
        {
            try
            {
                Log.Information("eMatch Process initiated.."+extract2Name);

                Dictionary<string, string> extract2 = new Dictionary<string, string>();

                //string headerLineFiledMatch = "GUID|CycleId|Statement Date|Product Name|Product Type|Statement Type|File Name|Customer Number|Customer Name 1|Address Line 1|Address Line 2|Postal Code|State|CIS Number|Card Number|Email Address|Statement Date|Delivery Method|Date of Birth/Registration|Combined Card Limit|Total Outstanding Balance|Final Status|CYCLE_ID|PRODUCT NAME|PRODUCT SUB NAME|DIVISION|STATEMENT DATE";
                string headerLineDataMatch = "Product Name|Product Type|Statement Date|Delivery Method|File Name|GUID|CycleId|Customer Name 1|E2 Customer Name 1|Address Line 1|E2 Address Line 1|Address Line 2|E2 Address Line 2|Postal Code|E2 Postal Code|State|E2 State|CIS Number|E2 CIS Number|Account Number|E2 Account Number|Date of Birth/Registration|E2 Date of Birth/Registration|Email Address|E2 Email Address|Loan Amount|Loan Amount|Final Status|Batch Type";


                //string headerLineMatcherLog = "UUID|CYCLE ID|PRODUCT NAME|STATEMENT DATE|MATCH STATUS";
                string outPutFolder = Path.GetDirectoryName(extract2Name).Replace("EXTRACT2", "MATCH");

                Directory.CreateDirectory(outPutFolder);

                //StreamWriter[] filesWriter = new StreamWriter[3];

                //filesWriter[0] = new StreamWriter(Path.Combine(Path.GetDirectoryName(extract2Name), "cc_fieldmatch.txt"));
                var filesWriter = new StreamWriter(Path.Combine(outPutFolder, fileName+"_datamatch.txt"));
                //filesWriter[2] = new StreamWriter(Path.Combine(Path.GetDirectoryName(extract2Name), "cc_match_log.txt"));
                string statusFile = Path.Combine(outPutFolder, fileName+"_exitcode.txt");


                //filesWriter[0].WriteLine(headerLineFiledMatch);
                filesWriter.WriteLine(headerLineDataMatch);
                //filesWriter[2].WriteLine(headerLineMatcherLog);


                using (FileStream fs = new FileStream(extract2Name, FileMode.Open))
                using (StreamReader rdr = new StreamReader(fs))
                {
                    bool header = false;
                    while (!rdr.EndOfStream)
                    {
                        string line = rdr.ReadLine();

                        if (!header)
                        {
                            header = true;
                            continue;
                        }

                        var value = line?.Split("|");

                        extract2.Add(value[5], line);

                    }

                    Log.Information("Initial count: "+extract2.Count());
                }

                using (FileStream fs = new FileStream(extract1Name, FileMode.Open))
                using (StreamReader rdr = new StreamReader(fs))
                {
                    bool recordStatus = true, finalStatus = true, header = false;

                    while (!rdr.EndOfStream)
                    {
                        var line = rdr.ReadLine();
                        
                        if (!header)
                        {
                            header = true;
                            continue;
                        }

                        var value = line?.Split("|");
                        //string cycleId = value[14];

                        //string fieldMatchLine = value[0];
                        string dataMatchLine = value[1];
                        //string productName = value[1];

                        if (extract2.ContainsKey(value[0]))
                        {
                            string extract2line = extract2.GetValueOrDefault(value[0]);

                            var extract2linesplit = extract2line?.Split("|");

                            //string matchLogLine = value[0]+"|"+extract2linesplit[14]+"|"+extract2linesplit[15]+"|"+extract2linesplit[18];

                            for (int i = 2; i<12; i++)
                            {
                                
                                if (value[i].Trim() == extract2linesplit[i+5].Trim())
                                {
                                    //fieldMatchLine=fieldMatchLine+"|0";
                                    dataMatchLine = dataMatchLine + "|"+value[i]+"|"+extract2linesplit[i+5];

                                }
                                else
                                {
                                    //fieldMatchLine=fieldMatchLine+"|1";
                                    dataMatchLine = dataMatchLine +"|"+value[i]+"|"+extract2linesplit[i+5];
                                    recordStatus = false;
                                }

                            }

                            if (recordStatus)
                            {
                                //fieldMatchLine= string.Concat(fieldMatchLine+"|0|", extract2linesplit[14], "|", extract2linesplit[15], "|", extract2linesplit[16], "|", extract2linesplit[17], "|", extract2linesplit[18]);
                                dataMatchLine = string.Join("|",extract2linesplit[0], extract2linesplit[1], extract2linesplit[2], extract2linesplit[3], extract2linesplit[4], extract2linesplit[5],dataMatchLine+$"|0|{batchType}|");
                                //matchLogLine = matchLogLine+"|0";

                            }
                            else
                            {
                                finalStatus=false;
                                recordStatus = true;
                                //fieldMatchLine =string.Concat(fieldMatchLine+"|1|", extract2linesplit[14], "|", extract2linesplit[15], "|", extract2linesplit[16], "|", extract2linesplit[17], "|", extract2linesplit[18]);
                                dataMatchLine = string.Join("|", extract2linesplit[0], extract2linesplit[1], extract2linesplit[2], extract2linesplit[3], extract2linesplit[4], extract2linesplit[5], dataMatchLine + $"|1|{batchType}|");
                                //matchLogLine = matchLogLine+"|1";
                            }


                            //filesWriter[0].WriteLine(fieldMatchLine);
                            filesWriter.WriteLine(dataMatchLine);
                            //filesWriter.WriteLine(matchLogLine);

                            extract2.Remove(value[0]);

                            if (extract2.Count() == 0)
                                break;
                        }

                    }

                    filesWriter.Close();

                    if (finalStatus)
                    {
                        if (extract2.Count() > 0)
                        {
                            Log.Information("Extract2 records not found in extract1.");
                            File.WriteAllText(statusFile, "1");
                        }
                        else

                            File.WriteAllText(statusFile, "0");
                    }
                    else
                        File.WriteAllText(statusFile, "1");

                }

                Log.Information("eMatch Process completed.."+extract2Name);

                var fileStatus = File.ReadAllText(statusFile);

                return fileStatus == "0"?true:false;
            }

            catch (Exception ex) 
            {
                Log.Error(ex,"Error while doing eMatch Process");

                return false;
            }
        }
    }
}
