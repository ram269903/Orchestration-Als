

namespace DSS.Als.Orchestration.Models
{
    public class StatementGeneration
    {
        public AlsInfo? AlsInfo { get; set; }
        public int DbRetryAttempts { get; set; }
        public int DbRetryWaitTime { get; set; }
    }

   

    public class AlsInfo
    {
        public ShellScriptPaths ShellScriptPaths { get; set; }
        public string OpsFilePath { get; set; }
        public InputFilePath InputFilePath { get; set; }

    }

   

    public class ShellScriptPaths
    {
        public string Doc1GenScript { get; set; }
    }

    public class InputFilePath
    {
        public string ASB { get; set; }
        public string TLWR { get; set; }
        public string TLWOR { get; set; }
        public string TLCC { get; set; }
        public string MGWR { get; set; }
        public string MGWOR { get; set; }
        public string MGEQT { get; set; }
        public string MGBBA { get; set; }
        public string PFVR { get; set; }
        public string PFFR { get; set; }
        public string RootPath { get; set; }
    }
}
