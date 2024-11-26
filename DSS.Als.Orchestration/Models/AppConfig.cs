using Common.Config;
using System.Collections.Generic;

namespace DSS.Als.Orchestration.Models
{
    public class AppConfig : AppSettings
    {
        public string DefaultJobSchedule { get; set; }
        public int DefaultBatchSize { get; set; }
        public Dictionary<string, JobSetting> JobSettings { get; set; }

        public string GetJobSchedule(JobSetting jobSetting)
        {
            var jobSchedule = DefaultJobSchedule;

            if (!string.IsNullOrEmpty(jobSetting?.JobSchedule))
                jobSchedule = jobSetting.JobSchedule;

            return jobSchedule;
        }

        public int GetJobBatchSize(JobSetting jobSetting)
        {
            var batchSize = DefaultBatchSize;

            if (jobSetting != null && jobSetting.BatchSize != null)
                batchSize = (int)jobSetting.BatchSize;

            return batchSize;
        }

        public JobSetting GetKeyValue(string key)
        {
            JobSetting value = null;

            if (JobSettings.ContainsKey(key))
                value = JobSettings[key];

            return value;
        }
    }

    public class JobSetting
    {
        public string JobSchedule { get; set; }
        public int? BatchSize { get; set; }
        public string Templates { get; set; }

        public int TimeInterval { get; set; }

        public int ProcessInternalWaitTime { get; set; }

        public bool dev { get; set; } = false;

        public int FoldersIgnoreDays { get; set; }
    }
}
