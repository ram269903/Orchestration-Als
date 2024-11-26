using Common.DataAccess.RDBMS.Model;
using Common.MicroBatch.Repository.Model;
using Common.Orchestration.Repository.Model;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.MicroBatch.Repository
{
    public interface IOrchestrationService
    {
        Task<MasterQueue> SaveQueueInformation(MasterQueue masterQueue);

        Task<MasterQueue> GetQueueInformation(string process);

        Task<SubQueue> SaveSubQueueInformation(SubQueue subQueue);

        Task<SubQueue> GetSubQueueInformation();

        Task<bool> InsertCsvRecordsToDatabase(string csvPath);

        Task<GenerateLog> InsertCsvRecordToDatabase(GenerateLog generateLog);

        Task<EmatchAttribute> SaveEmatchAttributes(EmatchAttribute ematchAttribute, string deliveryType,string? prtIndicator=null);

        //Task<EmatchAttribute> SaveFieldEmatch(EmatchAttribute ematchAttribute);
        Task<EmatchReport> SaveFieldEmatchReport(EmatchReport ematchReport, string deliveryType);

        Task<List<string>> GetDeliverySuppressAccountNumbers(string stmtDate);

        Task<List<string>> GetDeliverySuppressAccountNumbersCA(string stmtDate);

        Task<List<string>> GetArchiveSuppressAccountNumbers(string stmtDate);

        Task<List<string>> GetArchiveSuppressCisNumbers(string stmtDate);
        

        Task<List<string>> GetArchiveSuppressAccountNumbersCA(string stmtDate);

        Task<GenerateLog> SaveArsRecordMetaData(GenerateLog generateLog, string productName);

        Task<SuppressionReport> SaveSuppressionRecord(SuppressionReport suppressionReport);

        Task<string> UpdateProgressTable(string statusmessage, string cycleid);

        Task<string> UpdateBatchHistoryTable(string filedname, string productfullname, string statusmessage, string cycleid);

        Task<bool> LoadCSVFile(string filename, string tableName, List<DbColumn>? columns = null, string? separator = "|");

        Task<bool> LoadEmatchCSVFile(string filename, string tableName, List<DbColumn>? columns = null, string? separator = "|");

    }
}
