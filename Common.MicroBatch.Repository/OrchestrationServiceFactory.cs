using Common.Config;

namespace Common.MicroBatch.Repository
{
    public interface OrchestrationServiceFactory
    {
        public static IOrchestrationService GetMicroBatchService(DbConfig dbConfig)
        {
            switch (dbConfig.DataProvider.ToLower().Trim())
            {
                case "sqlserver": return new OrchestrationRepository(dbConfig);


                default: return new OrchestrationRepository(dbConfig);
            }
        }
        //public static IOrchestrationService GetArchiveService(DbConfig dbConfig)
        //{
        //    switch (dbConfig.DataProvider.ToLower().Trim())
        //    {
        //        case "sqlserver": return new OrchestrationRepository(dbConfig);


        //        default: return new OrchestrationRepository(dbConfig);
        //    }
        //}

    }
}
