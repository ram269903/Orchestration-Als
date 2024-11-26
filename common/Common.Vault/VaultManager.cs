//using e2NetRender;
//using e2NetRender.render2;
//using Microsoft.Extensions.Logging;
//using Common.Vault.Model;
//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Text;
//using System.Text.RegularExpressions;

//namespace Common.Vault
//{
//    public class VaultManager
//    {
//        private RenderClient2 vclient;
//        private readonly ILogger _logger;
//        private readonly VaultConfig _vaultConfig;

//        public VaultManager(VaultConfig vaultConfig, ILogger logger = null)
//        {
//            if (logger == null)
//                _logger = new LoggerFactory().CreateLogger("Vault");

//            vclient = new RenderClient2(vaultConfig.HostIp, vaultConfig.Port);
//            _vaultConfig = vaultConfig;
//        }

//        public List<Database> GetDatabaseList()
//        {
//            var repositories = new List<Database>();
//            var errorMessage = string.Empty;

//            try
//            {
//                vclient.connect();
//                e2DBList databaseList = vclient.DatabaseList();
//                vclient.close();

//                if (databaseList != null)
//                {
//                    for (int i = 0; i < databaseList.Size(); i++)
//                    {
//                        e2Database db = (e2Database)databaseList.Get(i);

//                        repositories.Add(new Database
//                        {
//                            Name = db.name,
//                            Description = db.desc
//                        });
//                    }
//                }
//                else
//                {
//                    _logger?.LogInformation("Failed to get database list : " + vclient.GetMyMsg());
//                }
//            }
//            catch (e2Exception ex)
//            {
//                _logger?.LogError(ex, "Failed to get database list - " + vclient.GetMyMsg());
//                throw new Exception(ex.Message + Environment.NewLine + vclient.GetMyMsg());
//            }
//            catch (Exception ex)
//            {
//                _logger?.LogInformation(ex, "Failed to get document");
//                throw;
//            }
//            finally
//            {
//                if (vclient.IsConnected())
//                    vclient.close();
//            }

//            return repositories;
//        }

//        public List<Index> GetIndexes(string database)
//        {
//            var indexes = new List<Index>();

//            try
//            {
//                vclient.connect();
//                e2IndexList idxl = vclient.DatabaseInfo(database);
//                vclient.close();

//                if (idxl != null)
//                {
//                    for (int i = 0; i < idxl.Size(); i++)
//                    {
//                        e2Index idx = (e2Index)idxl.Get(i);

//                        indexes.Add(new Index
//                        {
//                            IndexNo = idx.indexno,
//                            Name = idx.name,
//                            Attributes = idx.attributes,
//                            Flags = idx.flags,
//                            Description = idx.desc
//                        });
//                    }
//                }
//                else
//                {
//                    _logger?.LogInformation("Failed to get indexlist : " + vclient.GetMyMsg());
//                }
//            }
//            catch (e2Exception ex)
//            {
//                _logger?.LogError(ex, "Failed to get database list - " + vclient.GetMyMsg());
//                throw new Exception(ex.Message + Environment.NewLine + vclient.GetMyMsg());
//            }
//            catch (Exception ex)
//            {
//                _logger?.LogError(ex, "Failed to get document");
//                throw;
//            }
//            finally
//            {
//                if (vclient.IsConnected())
//                    vclient.close();
//            }

//            return indexes;
//        }

//        public List<SearchResult> SearchDatabase(string dbName, int indexNo, string indexflags)
//        {
//            var errorMessage = string.Empty;
//            List<SearchResult> searchResult = null;

//            try
//            {
//                e2IndexType indextype = e2IndexType.NONE;
//                indextype = e2Index.getIndexType(indexflags);

//                e2SearchParameters2 param2 = new e2SearchParameters2
//                {
//                    searchmode = e2SearchMode.GENERIC,
//                    dbname = dbName,
//                    prefix = "",
//                    first = "",
//                    maxresult = e2Consts.MAX_SEARCH

//                };
//                param2.SetIndex(indexNo, indexflags);

//                vclient.connect();

//                e2SearchList2 sl = vclient.DatabaseSearch(param2, out int moredata);

//                vclient.close();

//                if (sl == null)
//                {
//                    _logger?.LogInformation("Failed to get searchlist : " + vclient.GetMyMsg());
//                }

//                searchResult = new List<SearchResult>();

//                for (int i = 0; i < sl.Size(); i++)
//                {
//                    e2SearchData2 sd = (e2SearchData2)sl.Get(i);
//                    if (indextype == e2IndexType.CUSTOMER_RECORD || indextype == e2IndexType.DOCUMENT_RECORD)
//                    {
//                        searchResult.Add(new SearchResult
//                        {
//                            Matched = sd.matched,
//                            Account = sd.account,
//                            Date = sd.date,
//                            Format = sd.format,
//                            File = sd.file,
//                            Offset = sd.pointer,
//                            Pages = sd.pages
//                        });
//                    }
//                    //else if (indextype == e2IndexType.DOCUMENT_RECORD)
//                    //{
//                    //    searchResult.Add(new SearchResult
//                    //    {
//                    //        Matched = sd.matched,
//                    //        Account = sd.account,
//                    //        Date = sd.date,
//                    //        Format = sd.format,
//                    //        File = sd.file,
//                    //        Offset = sd.pointer,
//                    //        Pages = sd.pages
//                    //    });
//                    //}
//                    else
//                    {
//                        _logger?.LogInformation("Unknown index type [ " + indexflags + " ]");
//                    }
//                }
//            }
//            catch (e2Exception ex)
//            {
//                _logger?.LogError(ex, "Failed to get document - " + vclient.GetMyMsg());
//                throw new Exception(ex.Message + Environment.NewLine + vclient.GetMyMsg());
//            }
//            catch (Exception ex)
//            {
//                _logger?.LogError(ex, "Failed to get document");
//                throw;
//            }
//            finally
//            {
//                if (vclient.IsConnected())
//                    vclient.close();
//            }

//            return searchResult;
//        }

//        public bool CheckDocumentExists(string database, string documentId)
//        {
//            bool fileExists = false;

//            try
//            {
//                vclient.connect();

//                bool bret = vclient.DatabaseResolve(database, documentId, null, out string docfile, out string docoffset);

//                if (bret)
//                {
//                    //var documentDataList = vclient.DocumentData(docfile, docoffset);

//                    //if (documentDataList != null)
//                    //{
//                    fileExists = true;
//                    //}
//                    //else
//                    //{
//                    //    errorMessage = "Failed to get document information : " + vclient.GetMyMsg();
//                    //}
//                }
//                else
//                {
//                    _logger?.LogInformation("Failed to get document information : " + vclient.GetMyMsg());
//                }

//                vclient.close();
//            }
//            catch (e2Exception ex)
//            {
//                _logger?.LogInformation(ex, "Failed to get document - " + vclient.GetMyMsg());
//                throw new Exception(ex.Message + Environment.NewLine + vclient.GetMyMsg());
//            }
//            catch (Exception ex)
//            {
//                _logger?.LogError(ex, "Failed to get document");

//                throw;
//            }
//            finally
//            {
//                if (vclient.IsConnected())
//                    vclient.close();
//            }

//            return fileExists;

//        }

//        public string GetDocumentByFile(VaultQueryDocument vaultQueryDocument)
//        {
//            var errorMessage = string.Empty;
//            var documentPath = string.Empty;

//            try
//            {
//                var param = GetParameters(vaultQueryDocument);

//                if (param != null)
//                {
//                    param.transformmode = e2TransformMode.file;
//                    documentPath = Path.Combine(_vaultConfig.VaultDownloadFolder, param.outputfilename);
//                    param.outputfilename = documentPath;

//                    vclient.connect();

//                    var filesize = vclient.RenderTransformByFile(param);

//                    if (filesize <= 0)
//                        _logger?.LogInformation("Failed to get document pages by a file [ " + documentPath + " ] : " + vclient.GetMyMsg());

//                    vclient.close();

//                }
//            }
//            catch (e2Exception ex)
//            {
//                _logger?.LogError(ex, "Failed to get document - " + vclient.GetMyMsg());
//                throw new Exception(ex.Message + Environment.NewLine + vclient.GetMyMsg());
//            }
//            catch (Exception ex)
//            {
//                _logger?.LogError(ex, "Failed to get document");
//                throw;
//            }
//            finally
//            {
//                if (vclient.IsConnected())
//                    vclient.close();
//            }

//            return documentPath;

//        }

//        public string GetDocumentByFile(string fileVaultId, string fileName)
//        {
//            var documentPath = Path.Combine(_vaultConfig.VaultDownloadFolder, fileVaultId);

//            Directory.CreateDirectory(documentPath);

//            var documentFilePath = string.Empty;

//            try
//            {
//                vclient.connect();

//                e2RenderParameters param = new e2RenderParameters();

//                bool bret = vclient.DatabaseResolve(_vaultConfig.Database, fileVaultId, null, out string docfile, out string docoffset);

//                if (bret)
//                {
//                    var documentDataList = vclient.DocumentData(docfile, docoffset);

//                    param.totalpages = documentDataList.GetDocPages();

//                    param.SetOutputType((int)OutputFormat.raw);

//                    string fileType = documentDataList.GetDocType();

//                    // documentFilePath = Path.Combine(documentPath, docfile + fileType);
//                    documentFilePath = Path.Combine(documentPath, fileName);

//                    if (File.Exists(documentFilePath))
//                        return documentFilePath;

//                    for (int i = 1; i <= param.totalpages; i++)
//                    {
//                        e2DocumentPage fileData = vclient.DocumentPage(docfile, docoffset, i);

//                        byte[] data = fileData.pagedata;

//                        AppendAllBytes(documentFilePath, data);
//                    }
//                }
//                else
//                {
//                    _logger?.LogError("Failed to get document information : " + vclient.GetMyMsg());
//                }

//                vclient.close();
//            }
//            catch (e2Exception ex)
//            {
//                _logger?.LogError(ex, "Failed to get document - " + vclient.GetMyMsg());
//                //throw new Exception(ex.Message + Environment.NewLine + vclient.GetMyMsg());
//            }
//            catch (Exception ex)
//            {
//                _logger?.LogError(ex, "Failed to get document");
//                //throw;
//            }
//            finally
//            {
//                if (vclient.IsConnected())
//                    vclient.close();
//            }

//            return documentFilePath;
//        }

//        public byte[] GetDocumentInMemory(VaultQueryDocument vaultQueryDocument)
//        {
//            byte[] pagesBytes = null;

//            try
//            {
//                var param = GetParameters(vaultQueryDocument);

//                if (param != null)
//                {
//                    param.transformmode = e2TransformMode.mem;

//                    vclient.connect();

//                    var pages = vclient.RenderTransform(param);

//                    if (pages == null)
//                        _logger?.LogInformation("Failed to get document pages by memory : " + vclient.GetMyMsg());

//                    pagesBytes = pages.pagesdatabytes;

//                    vclient.close();

//                }
//            }
//            catch (e2Exception ex)
//            {
//                _logger?.LogError(ex, "Failed to get document - " + vclient.GetMyMsg());
//                throw new Exception(ex.Message + Environment.NewLine + vclient.GetMyMsg());
//            }
//            catch (Exception ex)
//            {
//                _logger?.LogError(ex, "Failed to get document");

//                throw;
//            }
//            finally
//            {
//                if (vclient.IsConnected())
//                    vclient.close();
//            }

//            return pagesBytes;

//        }

//        public string IngestFile(string filePath, string fileVaultId = null)
//        {
//            try
//            {
//                var errorMessage = string.Empty;

//                var readyFile = Path.Combine(_vaultConfig.StagingFolder, "ready");

//                if (!File.Exists(readyFile))
//                    File.Create(readyFile).Dispose();

//                if (string.IsNullOrEmpty(fileVaultId))
//                    fileVaultId = Regex.Replace(Convert.ToBase64String(Guid.NewGuid().ToByteArray()), "[/+=]", "");

//                var journalFilePath = GenerateJournalFile(filePath, fileVaultId);

//                var errorFolder = _vaultConfig.DropFolder + "\\collect.err";

//                if (Directory.Exists(errorFolder))
//                    Directory.Delete(errorFolder, true);

//                //using (new Impersonation(_vaultConfig.DropFolder, _vaultConfig.NetworkUserId, _vaultConfig.NetworkPassword))
//                //{
//                    string dropFolder = _vaultConfig.DropFolder + "\\collect";

//                    Directory.CreateDirectory(dropFolder);
//                    var fileName = Path.GetFileNameWithoutExtension(journalFilePath) + Path.GetExtension(filePath);

//                    //Copy the file to download folder
//                    //File.WriteAllBytes(dropFolder + "/" + Path.GetFileName(filePath), File.ReadAllBytes(filePath));
//                    File.WriteAllBytes(dropFolder + "/" + fileName, File.ReadAllBytes(filePath));

//                    //Copy the journal file to download folder
//                    File.WriteAllBytes(dropFolder + "/" + Path.GetFileName(journalFilePath), File.ReadAllBytes(journalFilePath));

//                    //Copy the ready file to download folder
//                    File.WriteAllBytes(dropFolder + "/ready", File.ReadAllBytes(readyFile));
//                    //using (var sw = new StreamWriter(dropFolder + "/" + "ready", true))
//                    //{
//                    //    sw.WriteLine("r");
//                    //    sw.Close();
//                    //}
//                //}

//                //var retrier = new Retrier<bool>();

//                //var response = retrier.Try(() => CheckDocumentExists(_vaultConfig.Database, fileVaultId), 12, true, 1000);

//                //if (!response)
//                //    return null;

//                return fileVaultId;
//            }
//            catch (Exception ex)
//            {
//                _logger?.LogError(ex, "Failed to injest document");

//                return null;
//            }
//        }

//        private e2RenderParameters GetParameters(VaultQueryDocument vaultQueryDocument)
//        {
//            e2RenderParameters param = null;

//            try
//            {
//                string db = vaultQueryDocument.Database;
//                string account = vaultQueryDocument.AccountNumber;

//                vclient.connect();

//                bool bret = vclient.DatabaseResolve(db, account, null, out string docfile, out string docoffset);

//                if (bret)
//                {
//                    var documentDataList = vclient.DocumentData(docfile, docoffset);

//                    if (documentDataList != null)
//                    {
//                        param = new e2RenderParameters
//                        {
//                            parametertype = e2DocParameterType.NORMAL,
//                            dbname = vaultQueryDocument.Database
//                        };

//                        param.SetNormalParameters(account, documentDataList.GetDocDate(), documentDataList.GetDocType(), docfile, docoffset);
//                        param.startpage = vaultQueryDocument.StartPage;
//                        param.totalpages = documentDataList.GetDocPages();

//                        param.SetOutputType((int)vaultQueryDocument.OutputFormat);
//                        //param.SetOutputType(documentDataList.GetFormat());
//                        param.resolution = vaultQueryDocument.Resolution;
//                        param.orientation = vaultQueryDocument.Orientation;
//                        param.outputfilename = docfile;
//                    }
//                    else
//                    {
//                        _logger?.LogInformation("Failed to get document information : " + vclient.GetMyMsg());
//                    }
//                }
//                else
//                {
//                    _logger?.LogInformation("Failed to get document information : " + vclient.GetMyMsg());
//                }

//                vclient.close();
//            }
//            catch (e2Exception ex)
//            {
//                _logger?.LogError(ex, "Failed to get document - " + vclient.GetMyMsg());
//                throw new Exception(ex.Message + Environment.NewLine + vclient.GetMyMsg());
//            }
//            catch (Exception ex)
//            {
//                _logger?.LogError(ex, "Failed to get document");
//                throw;
//            }
//            finally
//            {
//                if (vclient.IsConnected())
//                    vclient.close();
//            }

//            return param;
//        }

//        private string GenerateJournalFile(string filePath, string documentId)
//        {

//            var journalFileName = Path.GetFileNameWithoutExtension(filePath);
//            var JTimestamp = DateTime.Now.ToString("yyyyMMddHHmmssffff");
//            var journalFileExtention = Path.GetExtension(filePath);

//            var jobName = journalFileName + JTimestamp;
//            var jobDate = DateTime.Now.ToString("yyyyMMdd");

//            var journalBuilder = new StringBuilder();
//            journalBuilder.Append("J").Append("|").Append(jobName).Append("|").Append(jobDate).Append(Environment.NewLine);
//            journalBuilder.Append("D").Append("|").Append(documentId).Append("|").Append(jobDate).Append("|").Append(jobName + journalFileExtention);

//            var journalFile = Path.Combine(_vaultConfig.StagingFolder, jobName + ".jrn");

//            File.WriteAllText(journalFile, journalBuilder.ToString());

//            return journalFile;
//        }

//        private static void AppendAllBytes(string path, byte[] bytes)
//        {
//            using (var stream = new FileStream(path, FileMode.Append))
//            {
//                stream.Write(bytes, 0, bytes.Length);
//            }
//        }
//    }
//}
