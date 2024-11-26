using DSS.Als.Orchestration;
using DSS.Als.Orchestration.Models;
using Serilog.Events;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;
using Serilog.Core;
using Common.ArchiveOrchestration.Repository;
using Common.Config;
using Common.MicroBatch.Repository;
using Common.Security;
using Quartz;
using RHB.Als.Orchestration.Jobs;

namespace DSS.Orchestration
{
    public class Program
    {
        public static string _connectionString { get; private set; }
        public static void Main(string[] args)
        {
            const string loggerTemplate = @"{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u4}]<{ThreadId}> [{SourceContext:l}] {Message:lj}{NewLine}{Exception}";
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var logfile = Path.Combine(baseDir, "logs", "log_.log");

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .Enrich.With(new ThreadIdEnricher())
                .Enrich.FromLogContext()
                .WriteTo.Console(LogEventLevel.Information, loggerTemplate, theme: AnsiConsoleTheme.Literate)
                .WriteTo.File(logfile, LogEventLevel.Information, loggerTemplate,
                    rollingInterval: RollingInterval.Day, retainedFileCountLimit: null)
                .CreateLogger();

            try
            {
                Log.Information("Application starting up");
                Log.Information("====================================================================");
                Log.Information($"Application Starts. Version: {System.Reflection.Assembly.GetEntryAssembly()?.GetName().Version}");
                Log.Information($"Application Directory: {baseDir}");
                CreateHostBuilder(args).Build().Run();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Application start-up failed");
            }
            finally
            {
                Log.Information("====================================================================\r\n");
                Log.CloseAndFlush();
            }
        }


        public static IHostBuilder CreateHostBuilder(string[] args) =>
           Host.CreateDefaultBuilder(args)
               .UseSerilog()
               .ConfigureAppConfiguration((context, config) =>
               {
                   // Configure the app here.
               })
               .ConfigureServices((context, services) =>
               {

                   services.Configure<DbConfig>(context.Configuration.GetSection("DatabaseSettings"));
                   services.Configure<AppSettings>(context.Configuration.GetSection("AppSettings"));

                   services.Configure<FilePaths>(context.Configuration.GetSection("FilePaths"));
                   services.Configure<StatementGeneration>(context.Configuration.GetSection("StatementGeneration"));
                   services.Configure<EtlPaths>(context.Configuration.GetSection("EtlPaths"));


                   services.Configure<Archive>(context.Configuration.GetSection("Archive"));
                   services.Configure<DeliveryPaths>(context.Configuration.GetSection("DeliveryPaths"));
                   services.Configure<PrintSettings>(context.Configuration.GetSection("PrintSettings"));
                   services.Configure<AppConfig>(context.Configuration.GetSection("AppConfig"));

                   services.Configure<HAConfig>(context.Configuration.GetSection("HAConfig"));

                   var appConfig = context.Configuration.GetSection("AppConfig").Get<AppConfig>();

                   var dbConfig = context.Configuration.GetSection("DatabaseSettings").Get<DatabaseSettings>();
                   services.Configure<MailSettings>(context.Configuration.GetSection("MailSettings"));

                   var encPassword = string.Empty;

                   if (dbConfig.BOP.DataProvider.ToLower().Trim() == "mongodb")
                   {
                       var endIndex = dbConfig.BOP.ConnectionString.IndexOf("@");
                       var startIndex = dbConfig.BOP.ConnectionString.LastIndexOf(":", endIndex) + 1;
                       encPassword = dbConfig.BOP.ConnectionString.Substring(startIndex, endIndex - startIndex);
                   }
                   else
                       encPassword = dbConfig.BOP.ConnectionString.Split(';')[3].Substring(10);

                   var password = SecurityHelper.DecryptWithEmbedKey(encPassword, 15);
                   dbConfig.BOP.ConnectionString = dbConfig.BOP.ConnectionString.Replace(encPassword, password);
                   _connectionString = dbConfig.BOP.ConnectionString;

                   services.AddScoped(x => { return OrchestrationServiceFactory.GetMicroBatchService(dbConfig.BOP); });


                   if (dbConfig.Archive != null)
                   {

                       encPassword = dbConfig.Archive.ConnectionString.Split(';')[3].Substring(10);

                       password = SecurityHelper.DecryptWithEmbedKey(encPassword, 15);
                       dbConfig.Archive.ConnectionString = dbConfig.Archive.ConnectionString.Replace(encPassword, password);
                       _connectionString = dbConfig.Archive.ConnectionString;

                       services.AddScoped(x => { return ArchiveOrchestrationServiceFactory.GetArchiveService(dbConfig.Archive); });
                   }
                   services.AddQuartz(q =>
                   {
                       q.UseMicrosoftDependencyInjectionJobFactory();

                       #region Generation
                       //Create a "key" for the job

                       //var jkMicrobatchSchedular = new JobKey("MicrobatchSchedular");
                       //var jkGenerationSchedular = new JobKey("GenerationSchedular");


                       //q.AddJob<MicroBatchingSchedular>(opts => opts.WithIdentity(jkMicrobatchSchedular));
                       //q.AddJob<GeneratingSchedular>(opts => opts.WithIdentity(jkGenerationSchedular));

                       //q.AddTrigger(opts => opts
                       //    .ForJob(jkMicrobatchSchedular)
                       //    .WithIdentity("MicrobatchSchedular-trigger")
                       //    .WithCronSchedule(appConfig.JobSettings["Generating"].JobSchedule));
                       //q.AddTrigger(opts => opts
                       //    .ForJob(jkGenerationSchedular)
                       //    .WithIdentity("GenerationSchedular-trigger")
                       //    .WithCronSchedule(appConfig.JobSettings["Generating"].JobSchedule));



                       #endregion Generation

                       #region Print and Delivery
                       var jkPrintingSchedular = new JobKey("PrintingSchedular");
                       var jkDeliverySchedular = new JobKey("DeliverySchedular");

                       q.AddJob<PrintingSchedular>(opts => opts.WithIdentity(jkPrintingSchedular));
                       q.AddJob<DeliverySchedular>(opts => opts.WithIdentity(jkDeliverySchedular));

                       q.AddTrigger(opts => opts
                           .ForJob(jkPrintingSchedular)
                           .WithIdentity("PrintingSchedular-trigger")
                           .WithCronSchedule(appConfig.JobSettings["DeliverySchedular"].JobSchedule));
                       q.AddTrigger(opts => opts
                           .ForJob(jkDeliverySchedular)
                           .WithIdentity("DeliverySchedular-trigger")
                           .WithCronSchedule(appConfig.JobSettings["DeliverySchedular"].JobSchedule));

                       #endregion Print and Delivery

                       #region Archive
                       //var jkArchiveSchedular = new JobKey("ArchiveSchedular");

                       //q.AddJob<ArchivingSchedular>(opts => opts.WithIdentity(jkArchiveSchedular));

                       //q.AddTrigger(opts => opts
                       //    .ForJob(jkArchiveSchedular)
                       //    .WithIdentity("ArchiveSchedular-trigger")
                       //    .WithCronSchedule(appConfig.JobSettings["ArchiveSchedular"].JobSchedule));

                       #endregion Archive
                   });

                   services.AddQuartzHostedService(
                       q => q.WaitForJobsToComplete = true);

               });

        private static string GetJobSchedule(AppConfig appConfig, JobSetting jobSetting)
        {
            var jobSchedule = appConfig.DefaultJobSchedule;

            if (!string.IsNullOrEmpty(jobSetting?.JobSchedule))
                jobSchedule = jobSetting.JobSchedule;

            return jobSchedule;
        }

        private static JobSetting GetKeyValue(Dictionary<string, JobSetting> jobSettings, string key)
        {
            JobSetting value = null;

            if (jobSettings.ContainsKey(key))
                value = jobSettings[key];

            return value;
        }
    }

    class ThreadIdEnricher : ILogEventEnricher
    {
        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(
                    "ThreadId", Thread.CurrentThread.ManagedThreadId));
        }
    }
}

