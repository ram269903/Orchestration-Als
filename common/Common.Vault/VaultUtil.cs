using Common.Vault.Model;
using e2NetRender;
using e2NetRender.render2;
using e2NetRender.service;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace Common.Vault
{
    public class VaultUtil : IVaultUtil
    {
        private RenderClient2 vclient;
        private readonly ILogger _logger;
        private readonly VaultConfig _vaultConfig;

        public VaultUtil(IOptions<VaultConfig> vaultConfig, ILogger logger = null)
        {
            if (logger == null)
                _logger = new LoggerFactory().CreateLogger("Vault");
            
            _vaultConfig = vaultConfig.Value;

            vclient = new RenderClient2(_vaultConfig.HostIp, _vaultConfig.Port);
        }

        public VaultUtil(VaultConfig vaultConfig, ILogger logger = null)
        {
            if (logger == null)
                _logger = new LoggerFactory().CreateLogger("Vault");
            else
                _logger = logger;

            _vaultConfig = vaultConfig;

            vclient = new RenderClient2(_vaultConfig.HostIp, _vaultConfig.Port);
        }

        public bool CheckConnection()
        {
            var status = false;

            try
            {
                vclient.connect();
                status = vclient.IsConnected();
                vclient.close();
            }
            catch (Exception)
            {
                status = false;
            }
            finally
            {
                if (vclient.IsConnected())
                    vclient.close();
            }

            return status;
        }

        public IList<Database> GetDatabaseList()
        {
            var repositories = new List<Database>();
            var errorMessage = string.Empty;

            try
            {
                vclient.connect();
                e2DBList databaseList = vclient.DatabaseList();
                vclient.close();

                if (databaseList != null)
                {
                    for (int i = 0; i < databaseList.Size(); i++)
                    {
                        e2Database db = (e2Database)databaseList.Get(i);

                        repositories.Add(new Database
                        {
                            Name = db.name,
                            Description = db.desc
                        });
                    }
                }
                else
                {
                    _logger?.LogInformation("Failed to get database list : " + vclient.GetMyMsg());
                }
            }
            catch (e2Exception ex)
            {
                _logger?.LogError(ex, "Failed to get database list - " + vclient.GetMyMsg());
                throw new Exception(ex.Message + Environment.NewLine + vclient.GetMyMsg());
            }
            catch (Exception ex)
            {
                _logger?.LogInformation(ex, "Failed to get document");
                throw;
            }
            finally
            {
                if (vclient.IsConnected())
                    vclient.close();
            }

            return repositories;
        }

        public IList<Common.Vault.Model.Index> GetIndexes(string database)
        {
            var indexes = new List<Common.Vault.Model.Index> ();

            try
            {
                vclient.connect();
                e2IndexList idxl = vclient.DatabaseInfo(database);
                vclient.close();

                if (idxl != null)
                {
                    for (int i = 0; i < idxl.Size(); i++)
                    {
                        e2Index idx = (e2Index)idxl.Get(i);

                        indexes.Add(new  Common.Vault.Model.Index
                        {
                            IndexNo = idx.indexno,
                            Name = idx.name,
                            Attributes = idx.attributes,
                            Flags = idx.flags,
                            Description = idx.desc
                        });
                    }
                }
                else
                {
                    _logger?.LogInformation("Failed to get indexlist : " + vclient.GetMyMsg());
                }
            }
            catch (e2Exception ex)
            {
                _logger?.LogError(ex, "Failed to get database list - " + vclient.GetMyMsg());
                throw new Exception(ex.Message + Environment.NewLine + vclient.GetMyMsg());
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to get document");
                throw;
            }
            finally
            {
                if (vclient.IsConnected())
                    vclient.close();
            }

            return indexes;
        }

        public IList<SearchResult> SearchDatabase(string dbName, int indexNo, string indexflags)
        {
            var errorMessage = string.Empty;
            List<SearchResult> searchResult = null;

            try
            {
                e2IndexType indextype = e2IndexType.NONE;
                indextype = e2Index.getIndexType(indexflags);

                e2SearchParameters2 param2 = new e2SearchParameters2
                {
                    searchmode = e2SearchMode.GENERIC,
                    dbname = dbName,
                    prefix = "",
                    first = "",
                    maxresult = e2Consts.MAX_SEARCH

                };
                param2.SetIndex(indexNo, indexflags);

                vclient.connect();

                e2SearchList2 sl = vclient.DatabaseSearch(param2, out int moredata);

                vclient.close();

                if (sl == null)
                {
                    _logger?.LogInformation("Failed to get searchlist : " + vclient.GetMyMsg());
                }

                searchResult = new List<SearchResult>();

                for (int i = 0; i < sl.Size(); i++)
                {
                    e2SearchData2 sd = (e2SearchData2)sl.Get(i);
                    if (indextype == e2IndexType.CUSTOMER_RECORD || indextype == e2IndexType.DOCUMENT_RECORD)
                    {
                        searchResult.Add(new SearchResult
                        {
                            Matched = sd.matched,
                            Account = sd.account,
                            Date = sd.date,
                            Format = sd.format,
                            File = sd.file,
                            Offset = sd.pointer,
                            Pages = sd.pages
                        });
                    }
                    //else if (indextype == e2IndexType.DOCUMENT_RECORD)
                    //{
                    //    searchResult.Add(new SearchResult
                    //    {
                    //        Matched = sd.matched,
                    //        Account = sd.account,
                    //        Date = sd.date,
                    //        Format = sd.format,
                    //        File = sd.file,
                    //        Offset = sd.pointer,
                    //        Pages = sd.pages
                    //    });
                    //}
                    else
                    {
                        _logger?.LogInformation("Unknown index type [ " + indexflags + " ]");
                    }
                }
            }
            catch (e2Exception ex)
            {
                _logger?.LogError(ex, "Failed to get document - " + vclient.GetMyMsg());
                throw new Exception(ex.Message + Environment.NewLine + vclient.GetMyMsg());
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to get document");
                throw;
            }
            finally
            {
                if (vclient.IsConnected())
                    vclient.close();
            }

            return searchResult;
        }

        public bool CheckDocumentExists(string database, string documentId)
        {
            bool fileExists = false;

            try
            {
                vclient.connect();

                bool bret = vclient.DatabaseResolve(database, documentId, null, out string docfile, out string docoffset);

                if (bret)
                {
                    //var documentDataList = vclient.DocumentData(docfile, docoffset);

                    //if (documentDataList != null)
                    //{
                    fileExists = true;
                    //}
                    //else
                    //{
                    //    errorMessage = "Failed to get document information : " + vclient.GetMyMsg();
                    //}
                }
                else
                {
                    _logger?.LogInformation("Failed to get document information : " + vclient.GetMyMsg());
                }

                vclient.close();
            }
            catch (e2Exception ex)
            {
                _logger?.LogInformation(ex, "Failed to get document - " + vclient.GetMyMsg());
                throw new Exception(ex.Message + Environment.NewLine + vclient.GetMyMsg());
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to get document");

                throw;
            }
            finally
            {
                if (vclient.IsConnected())
                    vclient.close();
            }

            return fileExists;

        }

        public string GetDocumentByFile(VaultQueryDocument vaultQueryDocument)
        {
            var errorMessage = string.Empty;
            var documentPath = string.Empty;

            try
            {
                var param = GetParameters(vaultQueryDocument);

                if (param != null)
                {
                    param.transformmode = e2TransformMode.file;
                    documentPath = Path.Combine(_vaultConfig.VaultDownloadFolder, param.outputfilename);
                    param.outputfilename = documentPath;

                    vclient.connect();

                    var filesize = vclient.RenderTransformByFile(param);

                    if (filesize <= 0)
                        _logger?.LogInformation("Failed to get document pages by a file [ " + documentPath + " ] : " + vclient.GetMyMsg());

                    vclient.close();

                }
            }
            catch (e2Exception ex)
            {
                _logger?.LogError(ex, "Failed to get document - " + vclient.GetMyMsg());
                throw new Exception(ex.Message + Environment.NewLine + vclient.GetMyMsg());
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to get document");
                throw;
            }
            finally
            {
                if (vclient.IsConnected())
                    vclient.close();
            }

            return documentPath;

        }

        //public string GetDocumentByGuid(string guid, string documentPath, string fileName)
        //{
        //    //var documentPath = Path.Combine(_vaultConfig.VaultDownloadFolder, fileVaultId);

        //    Directory.CreateDirectory(documentPath);

        //    var documentFilePath = Path.Combine(documentPath, fileName);

        //    try
        //    {
        //        vclient.connect();

        //        e2RenderParameters param = new e2RenderParameters();
        //        e2DocumentList docList = vclient.DocumentListAll(_vaultConfig.Database.ToUpper(), guid, null, 10000);

        //        bool bret = vclient.DatabaseResolve(_vaultConfig.Database, guid, null, out string docfile, out string docoffset);

        //        if (bret)
        //        {
        //            var documentDataList = vclient.DocumentData(docfile, docoffset);

        //            param.totalpages = documentDataList.GetDocPages();

        //            param.SetOutputType((int)OutputFormat.raw);

        //            string fileType = documentDataList.GetDocType();

        //            // documentFilePath = Path.Combine(documentPath, docfile + fileType);
        //            documentFilePath = Path.Combine(documentPath, fileName);

        //            if (File.Exists(documentFilePath))
        //                return documentFilePath;

        //            for (int i = 1; i <= param.totalpages; i++)
        //            {
        //                e2DocumentPage fileData = vclient.DocumentPage(docfile, docoffset, i);

        //                byte[] data = fileData.pagedata;

        //                AppendAllBytes(documentFilePath, data);
        //            }
        //        }
        //        else
        //        {
        //            _logger?.LogError("Failed to get document information : " + vclient.GetMyMsg());
        //        }

        //        vclient.close();
        //    }
        //    catch (e2Exception ex)
        //    {
        //        _logger?.LogError(ex, "Failed to get document - " + vclient.GetMyMsg());
        //        //throw new Exception(ex.Message + Environment.NewLine + vclient.GetMyMsg());
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger?.LogError(ex, "Failed to get document");
        //        //throw;
        //    }
        //    finally
        //    {
        //        if (vclient.IsConnected())
        //            vclient.close();
        //    }

        //    return documentFilePath;
        //}

        //public string GetDocumentByGuid(string guid, string documentPath, string fileName)
        //{
        //    Directory.CreateDirectory(documentPath);

        //    var documentFilePath = Path.Combine(documentPath, fileName);

        //    try
        //    {
        //        vclient.connect();

        //        bool status = vclient.IsConnected();

        //        if (status == true)
        //        {
        //            bool bret = vclient.DatabaseResolve(_vaultConfig.Database, guid, null, out string docfile, out string docoffset);

        //            if (bret)
        //            {
        //                var docList = vclient.DocumentListAll(_vaultConfig.Database, guid, null, 10000);

        //                var data = (e2Document)docList.Get(0);

        //                if (docList != null && docList.Size() > 0)
        //                {

        //                    for (int i = 0; i < docList.Size(); i++)
        //                    {
        //                        var doc = (e2Document)docList.Get(i);
        //                        var docDate = DateTime.Parse(doc.date);

        //                        //if (docDate.Month == Month && docDate.Year == Year && doc.offset == offset && doc.file == fileData)
        //                        //{
        //                        SaveDocumentAsPdf(_vaultConfig.Database, doc, documentFilePath);
        //                        break;
        //                        //}
        //                    }
        //                }
        //                else
        //                {
        //                    _logger?.LogInformation("Failed to get document");
        //                    return "NoData";
        //                }
        //            }
        //            else
        //            {
        //                _logger?.LogError("Failed to get document information : " + vclient.GetMyMsg());
        //            }
        //        }

        //        vclient.close();
        //    }
        //    catch (e2Exception ex)
        //    {
        //        _logger?.LogError(ex, "Failed to get document - " + vclient.GetMyMsg());
        //        //throw new Exception(ex.Message + Environment.NewLine + vclient.GetMyMsg());
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger?.LogError(ex, "Failed to get document");
        //        //throw;
        //    }
        //    finally
        //    {
        //        if (vclient.IsConnected())
        //            vclient.close();
        //    }

        //    return documentFilePath;
        //}

        public byte[] GetDocumentByGuid(string guid)
        {
            byte[] pagesBytes = null;

            try
            {
                vclient.connect();

                bool status = vclient.IsConnected();

                if (status == true)
                {
                    var dparam = new e2RenderParameters
                    {
                        dbname = _vaultConfig.Database,
                        parametertype = e2DocParameterType.InstanceGUID,
                        startpage = 1,
                        totalpages = 9999,
                        outputtype = e2OutputType.PDF,
                        transformmode = e2TransformMode.mem,
                        orientation = 0, //1=90, 2=180, 3=270
                        resolution = 1024, //640, 800, 1024, 1280
                        background = 1, //0=no, 1=yes
                        cpix = 0,
                        cpiy = 0,
                    };

                    dparam.SetGUID(guid);

                    var pages = vclient.RenderTransform(dparam);

                    if (pages == null)
                        _logger?.LogInformation("Failed to get document pages by memory : " + vclient.GetMyMsg());

                    pagesBytes = pages.pagesdatabytes;

                }

                vclient.close();
            }
            catch (e2Exception ex)
            {
                _logger?.LogError(ex, "Failed to get document - " + vclient.GetMyMsg());
                //throw new Exception(ex.Message + Environment.NewLine + vclient.GetMyMsg());
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to get document");
                //throw;
            }
            finally
            {
                if (vclient.IsConnected())
                    vclient.close();
            }

            return pagesBytes;
        }

        public byte[] GetDocumentByAccountNumber(string guid)
        {
            byte[] pagesBytes = null;

            _logger?.LogInformation("Trying to document from vault with id: "+guid);

            try
            {
                vclient.connect();

                bool status = vclient.IsConnected();

                if (status == true)
                {
                    int moredoc = 1;
                    string pointer = "";
                    string file = "";

                    e2DocumentList doclist = vclient.DocumentList(_vaultConfig.Database, guid, "", 100, out moredoc);

                    e2Document doc = (e2Document)doclist.Get(doclist.Size() - 1);

                    bool bret = vclient.DatabaseResolve(_vaultConfig.Database, guid, "", out file, out pointer);

                    e2DocumentDataList datalist = vclient.DocumentData(file, pointer);
                    e2RenderParameters dparam = new e2RenderParameters();

                    string dl_acc = datalist.GetAccount();
                    string dl_date = datalist.GetDocDate();
                    string dl_type = datalist.GetDocType();
                    int dl_pages = datalist.GetDocPages();

                    dparam.parametertype = e2DocParameterType.NORMAL;

                    dparam.dbname = _vaultConfig.Database;
                    dparam.SetNormalParameters(dl_acc, dl_date, dl_type, file, pointer);
                    dparam.startpage = 1;
                    dparam.totalpages = 9999;//if you don't know the value, you can set it to 9999

                    dparam.outputtype = e2OutputType.PDF;//.RAW;
                    dparam.transformmode = e2TransformMode.mem;

                    dparam.orientation = 0;//1=90, 2=180, 3=270
                    dparam.resolution = 1024;//640, 800, 1024, 1280
                    dparam.background = 1;//0=no, 1=yes
                    dparam.cpix = 0;
                    dparam.cpiy = 0;
                    //dparam.depth=8;//32; //for TIFF output

                    //dparam.textencoding="";
                    //dparam.mark="";

                    e2RenderPages pagesdata = vclient.RenderTransform(dparam);
                                        
                    if (pagesdata != null)
                    {
                        _logger?.LogInformation("    success to render pages, " + Convert.ToString(pagesdata.pagesdatasize) + " bytes");
                    }
                    else
                    {
                        _logger?.LogInformation(" failed to render pages by memory : " + vclient.GetMyMsg());
                    }
                    
                    if (pagesdata == null)
                        _logger?.LogInformation("Failed to get document pages by memory : " + vclient.GetMyMsg());

                    pagesBytes = pagesdata.pagesdatabytes;

                }

                vclient.close();
            }
            catch (e2Exception ex)
            {
                _logger?.LogError(ex, "Failed to get document - " + vclient.GetMyMsg());
                //throw new Exception(ex.Message + Environment.NewLine + vclient.GetMyMsg());
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to get document");
                //throw;
            }
            finally
            {
                if (vclient.IsConnected())
                    vclient.close();
            }

            return pagesBytes;
        }

        //private void SaveDocumentAsPdf(string database, e2Document document, string filePath)
        //{
        //    try
        //    {

        //        var dparam = new e2RenderParameters
        //        {
        //            dbname = database,
        //            parametertype = e2DocParameterType.DocumentGUID,
        //            startpage = 1,
        //            totalpages = document.pages,
        //            outputtype = e2OutputType.PDF,
        //            transformmode = e2TransformMode.file,
        //            outputfilename = filePath,
        //            orientation = 0, //1=90, 2=180, 3=270
        //            resolution = 2048, //640, 800, 1024, 1280
        //            background = 1, //0=no, 1=yes
        //            cpix = 0,
        //            cpiy = 0,

        //        };

        //        //dparam.SetNormalParameters(document.account, document.date, document.type, document.file, document.offset);

        //        if (System.IO.File.Exists(filePath))
        //            System.IO.File.Delete(filePath);

        //        vclient.connect();

        //        int filesize = vclient.RenderTransformByFile(dparam);

        //        vclient.close();

        //        if (filesize <= 0)
        //            _logger?.LogError($"Failed to render document - {vclient.GetMyMsg()}");
        //    }
        //    catch (e2Exception ex)
        //    {
        //        _logger?.LogError(ex, $"Failed to render document - {vclient.GetMyMsg()}");
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger?.LogError(ex, "Failed to render document");
        //    }

        //}

        public byte[] GetDocumentInMemory(VaultQueryDocument vaultQueryDocument)
        {
            byte[] pagesBytes = null;

            try
            {
                var param = GetParameters(vaultQueryDocument);

                if (param != null)
                {
                    param.transformmode = e2TransformMode.mem;

                    vclient.connect();

                    var pages = vclient.RenderTransform(param);

                    if (pages == null)
                        _logger?.LogInformation("Failed to get document pages by memory : " + vclient.GetMyMsg());

                    pagesBytes = pages.pagesdatabytes;

                    vclient.close();

                }
            }
            catch (e2Exception ex)
            {
                _logger?.LogError(ex, "Failed to get document - " + vclient.GetMyMsg());
                throw new Exception(ex.Message + Environment.NewLine + vclient.GetMyMsg());
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to get document");

                throw;
            }
            finally
            {
                if (vclient.IsConnected())
                    vclient.close();
            }

            return pagesBytes;

        }

        public string IngestFile(string filePath, string fileVaultId = null)
        {
            try
            {
                var errorMessage = string.Empty;

                var readyFile = Path.Combine(_vaultConfig.StagingFolder, "ready");

                if (!File.Exists(readyFile))
                    File.Create(readyFile).Dispose();

                if (string.IsNullOrEmpty(fileVaultId))
                    fileVaultId = Regex.Replace(Convert.ToBase64String(Guid.NewGuid().ToByteArray()), "[/+=]", "");

                var journalFilePath = GenerateJournalFile(filePath, fileVaultId);

                var errorFolder = _vaultConfig.DropFolder + "\\collect.err";

                if (Directory.Exists(errorFolder))
                    Directory.Delete(errorFolder, true);

                //using (new Impersonation(_vaultConfig.DropFolder, _vaultConfig.NetworkUserId, _vaultConfig.NetworkPassword))
                //{
                    string dropFolder = _vaultConfig.DropFolder + "\\collect";

                    Directory.CreateDirectory(dropFolder);
                    var fileName = Path.GetFileNameWithoutExtension(journalFilePath) + Path.GetExtension(filePath);

                    //Copy the file to download folder
                    //File.WriteAllBytes(dropFolder + "/" + Path.GetFileName(filePath), File.ReadAllBytes(filePath));
                    File.WriteAllBytes(dropFolder + "/" + fileName, File.ReadAllBytes(filePath));

                    //Copy the journal file to download folder
                    File.WriteAllBytes(dropFolder + "/" + Path.GetFileName(journalFilePath), File.ReadAllBytes(journalFilePath));

                    //Copy the ready file to download folder
                    File.WriteAllBytes(dropFolder + "/ready", File.ReadAllBytes(readyFile));
                    //using (var sw = new StreamWriter(dropFolder + "/" + "ready", true))
                    //{
                    //    sw.WriteLine("r");
                    //    sw.Close();
                    //}
                //}

                //var retrier = new Retrier<bool>();

                //var response = retrier.Try(() => CheckDocumentExists(_vaultConfig.Database, fileVaultId), 12, true, 1000);

                //if (!response)
                //    return null;

                return fileVaultId;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to injest document");

                return null;
            }
        }

        private e2RenderParameters GetParameters(VaultQueryDocument vaultQueryDocument)
        {
            e2RenderParameters param = null;

            try
            {
                string db = vaultQueryDocument.Database;
                string account = vaultQueryDocument.AccountNumber;

                vclient.connect();

                bool bret = vclient.DatabaseResolve(db, account, null, out string docfile, out string docoffset);

                if (bret)
                {
                    var documentDataList = vclient.DocumentData(docfile, docoffset);

                    if (documentDataList != null)
                    {
                        param = new e2RenderParameters
                        {
                            parametertype = e2DocParameterType.NORMAL,
                            dbname = vaultQueryDocument.Database
                        };

                        param.SetNormalParameters(account, documentDataList.GetDocDate(), documentDataList.GetDocType(), docfile, docoffset);
                        param.startpage = vaultQueryDocument.StartPage;
                        param.totalpages = documentDataList.GetDocPages();

                        param.SetOutputType((int)vaultQueryDocument.OutputFormat);
                        //param.SetOutputType(documentDataList.GetFormat());
                        param.resolution = vaultQueryDocument.Resolution;
                        param.orientation = vaultQueryDocument.Orientation;
                        param.outputfilename = docfile;
                    }
                    else
                    {
                        _logger?.LogInformation("Failed to get document information : " + vclient.GetMyMsg());
                    }
                }
                else
                {
                    _logger?.LogInformation("Failed to get document information : " + vclient.GetMyMsg());
                }

                vclient.close();
            }
            catch (e2Exception ex)
            {
                _logger?.LogError(ex, "Failed to get document - " + vclient.GetMyMsg());
                throw new Exception(ex.Message + Environment.NewLine + vclient.GetMyMsg());
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to get document");
                throw;
            }
            finally
            {
                if (vclient.IsConnected())
                    vclient.close();
            }

            return param;
        }

        private string GenerateJournalFile(string filePath, string documentId)
        {

            var journalFileName = Path.GetFileNameWithoutExtension(filePath);
            var JTimestamp = DateTime.Now.ToString("yyyyMMddHHmmssffff");
            var journalFileExtention = Path.GetExtension(filePath);

            var jobName = journalFileName + JTimestamp;
            var jobDate = DateTime.Now.ToString("yyyyMMdd");

            var journalBuilder = new StringBuilder();
            journalBuilder.Append("J").Append("|").Append(jobName).Append("|").Append(jobDate).Append(Environment.NewLine);
            journalBuilder.Append("D").Append("|").Append(documentId).Append("|").Append(jobDate).Append("|").Append(jobName + journalFileExtention);

            var journalFile = Path.Combine(_vaultConfig.StagingFolder, jobName + ".jrn");

            File.WriteAllText(journalFile, journalBuilder.ToString());

            return journalFile;
        }

        private static void AppendAllBytes(string path, byte[] bytes)
        {
            using (var stream = new FileStream(path, FileMode.Append))
            {
                stream.Write(bytes, 0, bytes.Length);
            }
        }
    }
}
