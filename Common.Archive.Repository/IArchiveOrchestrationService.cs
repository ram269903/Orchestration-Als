using Common.ArchiveOrchestration.Repository.Model;
using Common.MicroBatch.Repository.Model;

namespace Common.ArchiveOrchestration.Repository
{
    public interface IArchiveOrchestrationService
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

        Task<List<string>> GetArchiveSuppressAccountNumbers(string stmtDate);

        Task<GenerateLog> SaveArsRecordMetaData(GenerateLog generateLog, string productName);

        Task<SuppressionReport> SaveSuppressionRecord(SuppressionReport suppressionReport);

        Task<string> UpdateProgressTable(string statusmessage, string cycleid);
    }
}
