using Common.Config;

namespace Common.ArchiveOrchestration.Repository
{
    public interface ArchiveOrchestrationServiceFactory
    {
        public static IArchiveOrchestrationService GetArchiveService(DbConfig dbConfig)
        {
            switch (dbConfig.DataProvider.ToLower().Trim())
            {
                case "sqlserver": return new ArchiveOrchestrationRepository(dbConfig);


                default: return new ArchiveOrchestrationRepository(dbConfig);
            }
        }

    }
}
