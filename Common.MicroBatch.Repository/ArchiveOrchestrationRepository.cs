using Common.Config;
using Common.DataAccess.MsSql;
using Common.DataAccess.RDBMS;
using Common.DataAccess.RDBMS.Model;
using Common.MicroBatch.Repository.Model;
using Common.Orchestration.Repository.Model;
using Microsoft.Extensions.Options;
using Serilog;
using System.Collections;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Common.MicroBatch.Repository
{
    public class ArchiveOrchestrationRepository : IOrchestrationService
    {
        private readonly QueryHelper _queryHelper = null;
        public ArchiveOrchestrationRepository(IOptions<DbConfig> dbConfig)
        {
            var databaseConfig = dbConfig.Value;

            _queryHelper = new QueryHelper(databaseConfig.ConnectionString);
        }

        public ArchiveOrchestrationRepository(DbConfig dbConfig)
        {
            _queryHelper = new QueryHelper(dbConfig.ConnectionString);
        }


        public async Task<MasterQueue> GetQueueInformation(string process)
        {
            //if (string.IsNullOrEmpty(id)) return null;

            string sql = $@"SELECT * FROM [dbo].[ccss_Main_Job_Queue] WHERE [status] = 'queue' and [Process]='{process}' order by statementdate asc";

            if (process == "")
                sql = @"SELECT * FROM [dbo].[ccss_Main_Job_Queue] WHERE [status] = 'queue' order by statementdate asc";

            //var parameters = new List<IDataParameter>
            //{
            //    QueryHelper.CreateSqlParameter("@id", Guid.Parse(id), SqlDbType.UniqueIdentifier)
            //};

            var masterQueue = (await _queryHelper.Read(sql, null, Make))?.FirstOrDefault();

            return masterQueue;
        }

        public async Task<MasterQueue> SaveQueueInformation(MasterQueue masterQueue)
        {
            if (string.IsNullOrEmpty(masterQueue.Id))
                return await InsertQueueInformation(masterQueue);
            else
                return await UpdateQueueInformation(masterQueue);
        }

        public async Task<SubQueue> SaveSubQueueInformation(SubQueue subQueue)
        {
            if (string.IsNullOrEmpty(subQueue.Id))
                return await InsertSubQueueInformation(subQueue);
            else
                return await UpdateSubQueueInformation(subQueue);
        }


        public async Task<MasterQueue> InsertQueueInformation(MasterQueue masterQueue)
        {
            const string sql = @"INSERT into [dbo].[ccss_Main_Job_Queue] (
                                [Id]
                               ,[Name]
                               ,[Job_Id]
                               ,[FilePath]
                               ,[Priority]
                               ,[Status]
                                ,[Process]
                                ,[StatementDate]
                                ,[Node]
                               ,[CreatedDate]
                               ,[UpdatedDate])
                            OUTPUT Inserted.ID
                            VALUES (
                                @id,
                                @name,
                                @jobId,
                                @filePath,
                                @priority,
                                @status,
                                @process,
                                @statementDate,
                                @node,
                                @createdDate,
                                @updatedDate)";

            var id = await _queryHelper.ExecuteScalar(sql, Take(masterQueue));

            masterQueue.Id = id.ToString();

            return masterQueue;
        }


        public async Task<MasterQueue> UpdateQueueInformation(MasterQueue masterQueue)
        {
            const string sql = @"update [dbo].[ccss_Main_Job_Queue] 
                                set 
                                [Status] = @status
                               ,[UpdatedDate] =@updatedDate
                                where id = @id";

            var id = await _queryHelper.ExecuteScalar(sql, Take(masterQueue));

            return masterQueue;
        }


        public async Task<SubQueue> GetSubQueueInformation()
        {
            // if (string.IsNullOrEmpty(id)) return null;

            const string sql = @"SELECT * FROM [dbo].[ccss_Sub_Job_Queue] where  [status] = 'queue' order by statementdate desc";

            //var parameters = new List<IDataParameter>
            //{
            //    QueryHelper.CreateSqlParameter("@id", Guid.Parse(id), SqlDbType.UniqueIdentifier)
            //};

            var subQueue = (await _queryHelper.Read(sql, null, MakeSubQueue))?.FirstOrDefault();

            return subQueue;
        }

        public async Task<SubQueue> InsertSubQueueInformation(SubQueue subQueue)
        {
            const string sql = @"INSERT into [dbo].[ccss_Sub_Job_Queue] (
                                [Id]
                               ,[Name]
                               ,[Job_Id]
                               ,[FilePath]
                               ,[Priority]
                               ,[Status]
                                ,[Process]
                                ,[StatementDate]
                                ,[Main_Job_Id]
                                ,[Node]
                               ,[CreatedDate]
                               ,[UpdatedDate])
                            OUTPUT Inserted.ID
                            VALUES (
                                @id,
                                @name,
                                @jobId,
                                @filePath,
                                @priority,
                                @status,
                                @process,
                                @statementDate,
                                @mainjobid,
                                @node,
                                @createdDate,
                                @updatedDate)";

            var id = await _queryHelper.ExecuteScalar(sql, TakeSubQueue(subQueue));

            return subQueue;
        }

        public async Task<SubQueue> UpdateSubQueueInformation(SubQueue subQueue)
        {
            const string sql = @"update [dbo].[ccss_Sub_Job_Queue] 
                                set 
                                [Status] = @status
                               ,[UpdatedDate] =@updatedDate
                                where id = @id";

            var id = await _queryHelper.ExecuteScalar(sql, TakeSubQueue(subQueue));

            return subQueue;
        }

        private static readonly Func<IDataReader, MasterQueue> Make = reader =>
            new MasterQueue
            {
                Id = reader["Id"].AsString(),
                Name = reader["Name"].AsString(),
                JobId = reader["Job_Id"].AsString(),
                FilePath = reader["FilePath"].AsString(),
                Priority = reader["Priority"].AsInt(),
                Status = reader["Status"].AsString(),
                Process = reader["Process"].AsString(),
                StatementDate = reader["StatementDate"].AsDateTime(),
                CreatedDate = reader.GetNullableDateTime("CreatedDate"),
                Node = reader["Node"].AsString(),
                UpdatedDate = reader.GetNullableDateTime("UpdatedDate")
            };

        private static readonly Func<IDataReader, SubQueue> MakeSubQueue = reader =>
            new SubQueue
            {
                Id = reader["Id"].AsString(),
                Name = reader["Name"].AsString(),
                JobId = reader["Job_Id"].AsString(),
                MainJobId = reader["Main_Job_Id"].AsString(),
                FilePath = reader["FilePath"].AsString(),
                Priority = reader["Priority"].AsInt(),
                Status = reader["Status"].AsString(),
                Process = reader["Process"].AsString(),
                StatementDate = reader["StatementDate"].AsDateTime(),
                CreatedDate = reader.GetNullableDateTime("CreatedDate"),
                Node = reader["Node"].AsString(),
                UpdatedDate = reader.GetNullableDateTime("UpdatedDate")
            };

        private static List<IDataParameter> Take(MasterQueue masterQueue)
        {
            if (string.IsNullOrEmpty(masterQueue.Id))
                masterQueue.Id = Guid.NewGuid().ToString();

            var parameters = new List<IDataParameter>
            {
                QueryHelper.CreateSqlParameter("@id", new Guid(masterQueue.Id), SqlDbType.UniqueIdentifier),
                QueryHelper.CreateSqlParameter("@jobId", masterQueue.JobId, SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@filePath", masterQueue.FilePath, SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@name", masterQueue.Name, SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@statementDate", masterQueue.StatementDate, SqlDbType.DateTime),
                QueryHelper.CreateSqlParameter("@priority", masterQueue.Priority, SqlDbType.Int),
                QueryHelper.CreateSqlParameter("@status", masterQueue.Status , SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@node", masterQueue.Node, SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@process", masterQueue.Process, SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@createdDate", masterQueue.CreatedDate, SqlDbType.DateTime2),
                QueryHelper.CreateSqlParameter("@updatedDate", DateTime.Now, SqlDbType.DateTime2)
            };

            return parameters;
        }

        private static List<IDataParameter> TakeSubQueue(SubQueue subQueue)
        {
            if (string.IsNullOrEmpty(subQueue.Id))
                subQueue.Id = Guid.NewGuid().ToString();

            var parameters = new List<IDataParameter>
            {
                QueryHelper.CreateSqlParameter("@id", new Guid(subQueue.Id), SqlDbType.UniqueIdentifier),
                QueryHelper.CreateSqlParameter("@jobId", subQueue.JobId, SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@mainjobid", subQueue.MainJobId, SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@filePath", subQueue.FilePath, SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@statementDate", subQueue.StatementDate, SqlDbType.DateTime2),
                QueryHelper.CreateSqlParameter("@name", subQueue.Name, SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@priority", subQueue.Priority, SqlDbType.Bit),
                QueryHelper.CreateSqlParameter("@status", subQueue.Status , SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@node", subQueue.Node, SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@process", subQueue.Process, SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@createdDate", subQueue.CreatedDate, SqlDbType.DateTime2),
                QueryHelper.CreateSqlParameter("@updatedDate", subQueue.UpdatedDate, SqlDbType.DateTime2)
            };

            return parameters;
        }

        public async Task<bool> InsertCsvRecordsToDatabase(string csvPath)
        {
            try
            {

                var dataTable = new DataTable("ccss_generation_report");

                string[] headers = "id, cycle_id,statement_date,report_type,product_name,statement_type,document_type,file_name,cust_name,cis_number,account_number,id_number,email_id,delivery_type,foreign_indicator,no_of_pages,status,fail_reason,start_time,end_time".Split(',');

                using (var stream = File.OpenRead(csvPath))
                {
                    using (var streamReader = new StreamReader(stream))
                    {
                        for (int i = 0; i < headers.Length; i++)
                        {
                            if (i == 17)
                                dataTable.Columns.Add(headers[i], typeof(DateTime));

                            else
                                dataTable.Columns.Add(headers[i], typeof(String));
                        }
                        while (!streamReader.EndOfStream)
                        {
                            var line = streamReader.ReadLine();

                            string[] rows = Regex.Split(line, "\\|(?=(?:[^\"]*\"[^\"]*\")*[^\']*[^\"]*$)");
                            DataRow dr = dataTable.NewRow();

                            for (int i = 0; i < headers.Length; i++)
                            {
                                var value = Convert.ToString(rows[i]).Trim();

                                if (i == 17)
                                {

                                    if (string.IsNullOrEmpty(value))
                                        dr[i] = DBNull.Value;
                                    else
                                        dr[i] = DateTime.ParseExact(value, "yyyyMMdd", CultureInfo.InvariantCulture);
                                }
                                else
                                    dr[i] = value;
                            }

                            dataTable.Rows.Add(dr);
                        }
                    }
                }

                _queryHelper.LoadDataTable(dataTable, "GenerateLog");

                return true;
            }

            catch (Exception ex)
            {
                return false;
            }

        }

        public async Task<GenerateLog> InsertCsvRecordToDatabase(GenerateLog generateLog)
        {
            const string sql = @" INSERT INTO [dbo].[ccss_generation_report]
                                        ([id],
                                         [cycle_id],
                                         [cust_name],
                                         [name_2],
                                         [addr_1],
                                         [addr_2],
                                         [addr_3],
                                         [postcode],
                                         [state],
                                         [account_number],
                                         [cis_number],
                                         [id_number],
                                         [product_name],
                                         [bank_code],
                                         [document_type],
                                         [staff],
                                         [division],
                                         [premier],
                                         [entity],
                                         [email_id],
                                         [statement_date],
                                         [delivery_method],
                                         [dob],
                                         [opening_balance],
                                         [ending_balance],
                                         [no_of_pages],
                                         [product_code],
                                         [Report_Type],
                                         [File_Name],
                                         [foreign_indicator],
										 [Status],
										 [Created_Date],
										 [Updated_Date],
                                         [Batch_Type])
                            output      inserted.id
                            VALUES      ( @id,
                                          @cycle_id,
                                          @cust_name,
                                          @name_2,
                                          @addr_1,
                                          @addr_2,
                                          @addr_3,
                                          @postcode,
                                          @state,
                                          @account_number,
                                          @cis_number,
                                          @id_number,
                                          @product_name,
                                          @bank_code,
                                          @document_type,
                                          @staff,
                                          @division,
                                          @premier,
                                          @entity,
                                          @email_id,
                                          @statement_date,
                                          @delivery_method,
                                          @dob,
                                          @opening_balance,
                                          @ending_balance,
                                          @no_of_pages,
                                          @product_code,
										  @report_type,
                                          @file_name,
                                          @foreign_indicator,
										  @status,
										  @created_date,
										  @updated_date,
                                          'NM'
										  )";

            var id = await _queryHelper.ExecuteScalar(sql, TakeGenerateLog(generateLog));

            return generateLog;
        }

        private List<IDataParameter> TakeGenerateLog(GenerateLog log)
        {

            var parameters = new List<IDataParameter>
            {
                QueryHelper.CreateSqlParameter("@id", new Guid(log.Id), SqlDbType.UniqueIdentifier),
                QueryHelper.CreateSqlParameter("@cycle_id",log.CycleID,SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@cust_name",log.Name_1,SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@name_2",log.Name_2,SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@addr_1",log.Address1,SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@addr_2",log.Address2,SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@addr_3",log.Address3,SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@postcode",log.PostCode,SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@state",log.State,SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@account_number",log.AccountNumber,SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@cis_number",log.CISNumber,SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@id_number",log.IdNumber,SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@product_name",log.ProductName,SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@bank_code",log.BankCode,SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@document_type",log.DocumentType,SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@staff",log.Staff,SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@division",log.Division,SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@premier",log.Premier,SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@entity",log.Entity,SqlDbType.NVarChar),
                QueryHelper. CreateSqlParameter("@email_id",log.Email,SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@statement_date",log.StatementDate,SqlDbType.DateTime2),
                QueryHelper.CreateSqlParameter("@delivery_method",log.DeliveryMethod,SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@dob",log.DOB,SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@opening_balance",log.LoanAmount,SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@ending_balance",log.LoanType,SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@no_of_pages",log.NoOfPages,SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@product_code",log.ProductCode,SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@status",log.Status,SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@report_type",log.ReportType,SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@file_name",log?.FileName,SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@foreign_indicator",log?.PrintingVendorIndicator,SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@created_date",log.CreatedDate,SqlDbType.DateTime2),
                QueryHelper.CreateSqlParameter("@updated_date",log.UpdatedDate,SqlDbType.DateTime2)
            };

            return parameters;
        }


        public async Task<EmatchAttribute> SaveEmatchAttributes(EmatchAttribute ematchAttribute, string deliveryType, string? prtIndicator=null)
        {
           
                string sql = $@"INSERT INTO [dbo].[ccss_ematch_{deliveryType}_attr_report]
                                       ([id]
                                       ,[cycle_id]
                                       ,[statement_date]
                                       ,[file_name]
                                       ,[product_name]
                                       ,[product_desc]
                                       ,[statement_type]
                                       ,[delivery_method]
                                       ,[e1_account_number]
                                       ,[e2_account_number]
                                       ,[e1_cis_number]
                                       ,[e2_cis_number]
                                       ,[e1_customer_name]
                                       ,[e2_customer_name]
                                       ,[e1_address_line1]
                                       ,[e2_address_line1]
                                       ,[e1_address_line2]
                                       ,[e2_address_line2]
                                       ,[e1_address_line3]
                                       ,[e2_address_line3]
                                       ,[e1_postcode]
                                       ,[e2_postcode]
                                       ,[e1_state]
                                       ,[e2_state]
                                       ,[e1_dob]
                                       ,[e2_dob]
                                       ,[e1_email_address]
                                       ,[e2_email_address]
                                       ,[e1_beg_balance]
                                       ,[e2_beg_balance]
                                       ,[e1_end_balance]
                                       ,[e2_end_balance]
                                       ,[remarks]
                                       ,[created_date]
                                       ,[updated_date]
                                       ,[extra_1]
                                       ,[extra_2]
                                       ,[extra_3]
                                       ,[extra_4]
                                        ,[Batch_Type])
                                 VALUES
                                       (
		                                @id
                                       ,@cycle_id
                                       ,@statement_date
                                       ,@file_name
                                       ,@product_name
                                       ,@product_desc
                                       ,@statement_type
                                       ,@delivery_method
                                       ,@e1_account_number
                                       ,@e2_account_number
                                       ,@e1_cis_number
                                       ,@e2_cis_number
                                       ,@e1_customer_name
                                       ,@e2_customer_name
                                       ,@e1_address_line1
                                       ,@e2_address_line1
                                       ,@e1_address_line2
                                       ,@e2_address_line2
                                       ,@e1_address_line3
                                       ,@e2_address_line3
                                       ,@e1_postcode
                                       ,@e2_postcode
                                       ,@e1_state
                                       ,@e2_state
                                       ,@e1_dob
                                       ,@e2_dob
                                       ,@e1_email_address
                                       ,@e2_email_address
                                       ,@e1_beg_balance
                                       ,@e2_beg_balance
                                       ,@e1_end_balance
                                       ,@e2_end_balance
                                       ,@remarks
                                       ,@created_date
                                       ,@updated_date
                                       ,@extra_1
                                       ,@extra_2
                                       ,@extra_3
                                       ,@extra_4
                                        ,'NM')";

                if (deliveryType == "prt")
                
                    sql = $@"INSERT INTO [dbo].[ccss_ematch_{deliveryType}_attr_report]
                                       ([id]
                                       ,[cycle_id]
                                       ,[statement_date]
                                       ,[file_name]
                                       ,[product_name]
                                       ,[product_desc]
                                       ,[statement_type]
                                       ,[delivery_method]
                                       ,[e1_account_number]
                                       ,[e2_account_number]
                                       ,[e1_cis_number]
                                       ,[e2_cis_number]
                                       ,[e1_customer_name]
                                       ,[e2_customer_name]
                                       ,[e1_address_line1]
                                       ,[e2_address_line1]
                                       ,[e1_address_line2]
                                       ,[e2_address_line2]
                                       ,[e1_address_line3]
                                       ,[e2_address_line3]
                                       ,[e1_postcode]
                                       ,[e2_postcode]
                                       ,[e1_state]
                                       ,[e2_state]
                                       ,[e1_dob]
                                       ,[e2_dob]
                                       ,[e1_beg_balance]
                                       ,[e2_beg_balance]
                                       ,[e1_end_balance]
                                       ,[e2_end_balance]
                                        ,[foreign_indicator]
                                       ,[remarks]
                                       ,[created_date]
                                       ,[updated_date]
                                       ,[extra_1]
                                       ,[extra_2]
                                       ,[extra_3]
                                       ,[extra_4]
                                        ,[Batch_Type])
                                 VALUES
                                       (
		                                @id
                                       ,@cycle_id
                                       ,@statement_date
                                       ,@file_name
                                       ,@product_name
                                       ,@product_desc
                                       ,@statement_type
                                       ,@delivery_method
                                       ,@e1_account_number
                                       ,@e2_account_number
                                       ,@e1_cis_number
                                       ,@e2_cis_number
                                       ,@e1_customer_name
                                       ,@e2_customer_name
                                       ,@e1_address_line1
                                       ,@e2_address_line1
                                       ,@e1_address_line2
                                       ,@e2_address_line2
                                       ,@e1_address_line3
                                       ,@e2_address_line3
                                       ,@e1_postcode
                                       ,@e2_postcode
                                       ,@e1_state
                                       ,@e2_state
                                       ,@e1_dob
                                       ,@e2_dob
                                       ,@e1_beg_balance
                                       ,@e2_beg_balance
                                       ,@e1_end_balance
                                       ,@e2_end_balance
                                        ,@foreign_indicator
                                       ,@remarks
                                       ,@created_date
                                       ,@updated_date
                                       ,@extra_1
                                       ,@extra_2
                                       ,@extra_3
                                       ,@extra_4
                                        ,'NM')";

                    _ = await _queryHelper.ExecuteScalar(sql, TakeAttributeReport(ematchAttribute, prtIndicator));
                
                   // _ = await _queryHelper.ExecuteScalar(sql, TakeAttributeReport(ematchAttribute));

                return ematchAttribute;
            
            
        }

        private List<IDataParameter> TakeAttributeReport(EmatchAttribute report, string? printIndicator=null)
        {
            if (string.IsNullOrEmpty(report.Id))
                report.Id = Guid.NewGuid().ToString();

            var parameters = new List<IDataParameter>
            {
                QueryHelper.CreateSqlParameter("@id", new Guid(report.Id), SqlDbType.UniqueIdentifier),
                QueryHelper.CreateSqlParameter("@cycle_id", report.CycleId, SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@statement_date", report.StatementDate, SqlDbType.DateTime2),
                QueryHelper.CreateSqlParameter("@file_name", report.FileName, SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@product_name", report.ProductName , SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@product_desc", report.ProductDesc ,SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@statement_type", report.StatementType, SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@delivery_method", report.DeliveryMethod, SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@e1_account_number", report.E1AccountNumber, SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@e2_account_number", report.E2AccountNumber, SqlDbType.NVarChar),

                QueryHelper.CreateSqlParameter("@e1_cis_number", report.E1CisNumber, SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@e2_cis_number", report.E2CisNumber, SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@e1_customer_name", report.E1CustomerName, SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@e2_customer_name", report.E2CustomerName, SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@e1_address_line1", report.E1AddressLine1 , SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@e2_address_line1", report.E2AddressLine1, SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@e1_address_line2", report.E1AddressLine2, SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@e2_address_line2", report.E2AddressLine2, SqlDbType.NVarChar),

                QueryHelper.CreateSqlParameter("@e1_address_line3", report.E1AddressLine3, SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@e2_address_line3", report.E2AddressLine3, SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@e1_postcode", report.E1Postcode, SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@e2_postcode", report.E2Postcode , SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@e1_state", report.E1State , SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@e2_state", report.E2State, SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@e1_dob", report.E1Dob, SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@e2_dob", report.E2Dob, SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@e1_email_address", report.E1EmailAddress, SqlDbType.NVarChar),

                QueryHelper.CreateSqlParameter("@e2_email_address", report.E2EmailAddress, SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@e1_beg_balance", report.E1BegBalance, SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@e2_beg_balance", report.E2BegBalance, SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@e1_end_balance", report.E1EndBalance , SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@e2_end_balance", report.E2EndBalance , SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@status", report.Status, SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@remarks", report.Remarks, SqlDbType.NVarChar),
                
                QueryHelper.CreateSqlParameter("@foreign_indicator", printIndicator ==  null?"":printIndicator, SqlDbType.NVarChar),
                
                QueryHelper.CreateSqlParameter("@statementType", report.StatementType, SqlDbType.NVarChar),

                QueryHelper.CreateSqlParameter("@created_date", report.CreatedDate, SqlDbType.DateTime2),
                QueryHelper.CreateSqlParameter("@updated_date", report.UpdatedDate, SqlDbType.DateTime2),
                QueryHelper.CreateSqlParameter("@extra_1", report.Extra1, SqlDbType.NVarChar),
                
                QueryHelper.CreateSqlParameter("@extra_2", report.Extra2, SqlDbType.NVarChar),

                QueryHelper.CreateSqlParameter("@extra_3", report.Extra3, SqlDbType.NVarChar),

                QueryHelper.CreateSqlParameter("@extra_4", report.Extra4, SqlDbType.NVarChar)

            };

            return parameters;
        }

        //public async Task<EmatchReport> SaveFieldEmatch(EmatchReport ematchReport)
        //{
        //    const string sql = @"INSERT [dbo].[ccss_ematch_ars] (
        //                        [Id],
        //                        [Cycle_Id],
        //                        [Statement_Date],
								//[File_Name],
								//[Product_Name],
								//[Statement_type],
        //                        [Data_Match],
								//[Remarks],
        //                        [Created_date],
        //                        [Updated_date],
        //                        [extr_1],
								//[extr_2],
								//[extr_3],
								//[extr_4])
        //                    VALUES (
							 //   @id
        //                        @cycle_Id,
        //                        @statement_Date,
        //                        @file_Name,
        //                        @product_Name,
								//@statement_type,
        //                        @data_Match,
								//@remarks,
								//@created_date,
								//@updated_date,
								//@extr_1,
								//@extr_2,
								//@extr_3,
								//@extr_4)";

        //    var id = await _queryHelper.ExecuteScalar(sql, Take(ematchReport));

        //    return ematchReport;
        //}

        private List<IDataParameter> Take(EmatchReport report)
        {
            if (string.IsNullOrEmpty(report.Id))
                report.Id = Guid.NewGuid().ToString();

            var parameters = new List<IDataParameter>
            {

                QueryHelper.CreateSqlParameter("@id", new Guid(report.Id), SqlDbType.UniqueIdentifier),
                QueryHelper.CreateSqlParameter("@cycle_id", report.CycleId, SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@statement_Date", report.StatementDate, SqlDbType.DateTime2),
                QueryHelper.CreateSqlParameter("@file_Name", report.FileName, SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@product_Name", report.ProductName, SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@statement_type", report.StatementType, SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@data_Match", report.DataMatch, SqlDbType.NVarChar),

                QueryHelper.CreateSqlParameter("@acc_num", report.AccNum, SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@cis_num", report.CisNum, SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@product_desc", report.ProductDesc, SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@delivery_method", report.DeliveryMethod, SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@account_number", report.AccountNumber, SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@cis_number", report.CisNumber, SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@customer_name", report.CustomerName, SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@address_line1", report.AddressLine1, SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@address_line2", report.AddressLine2, SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@address_line3", report.AddressLine3, SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@Postcode", report.Postcode, SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@State", report.State, SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@dob", report.Dob, SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@beg_balance", report.BegBalance, SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@end_balance", report.EndBalance, SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@email_address", report.EmailAddress, SqlDbType.NVarChar),

                QueryHelper.CreateSqlParameter("@foreign_indicator", report.ForeignIndicator, SqlDbType.NVarChar),

                QueryHelper.CreateSqlParameter("@Remarks", report.Remarks, SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@Created_date", report.CreatedDate, SqlDbType.DateTime2),
                QueryHelper.CreateSqlParameter("@Updated_date", report.UpdatedDate, SqlDbType.DateTime2)

            };
            return parameters;
        }

        public async Task<EmatchReport> SaveFieldEmatchReport(EmatchReport ematchReport, string deliveryType)
        {
             string sql = $@"INSERT into [dbo].[ccss_ematch_{deliveryType}_report] (
                                [id],
                                [cycle_id],
                                [statement_date],
                                [file_name],
                                [acc_num],
                                [cis_num],
                                [product_name],
                                [product_desc],
                                [statement_type],
                                [delivery_method],
                                [account_number],
                                [cis_number],
                                [customer_name],
                                [address_line1],
                                [address_line2],
                                [address_line3],
                                [postcode],
                                [state],
                                [dob],
                                [beg_balance],
                                [end_balance],
                                [email_address],
                                [data_match],
                                [remarks],
                                [created_date],
                                [updated_date],
                                [Batch_Type])
                            VALUES (
                                @id,
                                @cycle_Id,
                                @statement_Date,
                                @file_Name,
                                @acc_num,
								@cis_num,
								@product_Name,
								@product_desc,
				                @statement_type,
                                @delivery_method,
                                @account_number,
                                @cis_number,
                                @customer_name,
                                @address_line1,
                                @address_line2,
                                @address_line3,
                                @postcode,
                                @state,
                                @dob,
                                @beg_balance,
                                @end_balance,
                                @email_address,
                                @data_match,
                                @remarks,
                                @created_date,
                                @updated_date,
                                'NM')";

            if(deliveryType == "prt")
                sql = $@"INSERT into [dbo].[ccss_ematch_{deliveryType}_report] (
                                [id],
                                [cycle_id],
                                [statement_date],
                                [file_name],
                                [acc_num],
                                [cis_num],
                                [product_name],
                                [product_desc],
                                [statement_type],
                                [delivery_method],
                                [account_number],
                                [cis_number],
                                [customer_name],
                                [address_line1],
                                [address_line2],
                                [address_line3],
                                [postcode],
                                [state],
                                [dob],
                                [beg_balance],
                                [end_balance],
                                [data_match],
                                [foreign_indicator],
                                [remarks],
                                [created_date],
                                [updated_date],
                                [Batch_Type])
                            VALUES (
                                @id,
                                @cycle_Id,
                                @statement_Date,
                                @file_Name,
                                @acc_num,
								@cis_num,
								@product_Name,
								@product_desc,
				                @statement_type,
                                @delivery_method,
                                @account_number,
                                @cis_number,
                                @customer_name,
                                @address_line1,
                                @address_line2,
                                @address_line3,
                                @postcode,
                                @state,
                                @dob,
                                @beg_balance,
                                @end_balance,
                                @data_match,
                                @foreign_indicator,
                                @remarks,
                                @created_date,
                                @updated_date,
                                'NM')";

            var id = await _queryHelper.ExecuteScalar(sql, Take(ematchReport));

            return ematchReport;
        }


        public async Task<List<string>> GetDeliverySuppressAccountNumbers(string date) {
            
            var stmtDate = DateTime.ParseExact(date, "yyyyMMdd", CultureInfo.InvariantCulture).ToString("yyyy-MM-dd");

            string sql = $@"SELECT * FROM [dbo].[ccss_stmt_suppress] where cast (statement_date as date) = '{stmtDate}' and [Status]='1'";

            var data = (await _queryHelper.Read(sql, null, MakeDeliveryAccountNumbers));

            return data.ToList();
        
        }

        private static readonly Func<IDataReader, string> MakeDeliveryAccountNumbers = reader => reader["account_number"].AsString();

        public async Task<List<string>> GetArchiveSuppressCisNumbers(string date)
        {
            var stmtDate = DateTime.ParseExact(date, "yyyyMMdd", CultureInfo.InvariantCulture).ToString("yyyy-MM-dd");

            string sql = $@"SELECT * FROM [dbo].[ccss_stmt_suppress] where cast (statement_date as date) = '{stmtDate}' and [Status]='1' and product_name='Savings Account'";


            var data = await _queryHelper.Read(sql, null, MakeArchiveCisNumbers);

            return data.ToList();

        }

        public async Task<List<string>> GetArchiveSuppressAccountNumbers(string date)
        {
            var stmtDate = DateTime.ParseExact(date, "yyyyMMdd", CultureInfo.InvariantCulture).ToString("yyyy-MM-dd");

            //string sql = $@"SELECT * FROM [dbo].[ccss_stmt_suppress] where cast (statement_date as date) = '{stmtDate}' and [Status]='1'";

            string sql = $@"SELECT * FROM VW_Suppression where cast (statement_date as date) = '{stmtDate}' and [Status]='1'";

            var data = await _queryHelper.Read(sql, null, MakeArchiveAccountNumbers);

            return data.ToList();
        }

        public async Task<GenerateLog> SaveArsRecordMetaData(GenerateLog generateLog,string productName)
        {
             string sql = $@" INSERT INTO [dbo].[ccss_ars_metadata_{productName}]
                                        ([id],
                                         [cycle_id],
                                         [customer_name],
                                         [address_line1],
                                         [address_line2],
                                         [address_line3],
                                         [postcode],
                                         [state],
										 [statement_date],
										 [product_code],
										 [product_name],
										 [product_name_s],
										 [bank_code],
                                         [bank_code_s],
										 [document_type],
                                         [account_number],
                                         [cis_number],
										 [dob],
                                         [id_number],
										 [division],
										 [division_s],
                                         [premier],
										 [premier_s],
                                         [staff],
										 [staff_s],
                                         [entity],
										 [entity_s],
										 [email_address],
										 [no_of_pages],
										 [delivery_method],
										 [delivery_method_s],										 
                                         [doc_id],
										 [doc_master_id],
										 [doc_instance_id],
										 [vendor_id],
										 [doc_type_id],
										 [file_name],
										 [printing_vendor_indicator],
										 [remarks],
										 [isdeleted],
										 [isdisabled],
										 [status],
										 [created_date],
										 [updated_date],
										 [Batch_Type]
                                         )
                            output      inserted.id
                            VALUES      ( @id,
                                          @cycle_id,
                                          @cust_name,
                                          @addr_1,
                                          @addr_2,
                                          @addr_3,
                                          @postcode,
                                          @state,
                                          @statement_date,
                                          @product_code,
                                          @product_name,
										  @product_name_s,
                                          @bank_code,
										  @bank_code_s,
                                          @document_type,
                                          @account_number,
                                          @cis_number,
                                          @dob,
                                          @id_number,
                                          @division,
										  @division_s,
                                          @premier,
										  @premier_s,
                                          @staff,
										  @staff_s,
                                          @entity,
										  @entity_s,
                                          @email_address,
                                          @no_of_pages,
                                          @delivery_method,
										  @delivery_method_s,
										  @doc_id,
										  @doc_master_id,
										  @doc_instance_id,
										  @vendor_id,
										  @doc_type_id,
										  @file_name,
										  @printing_vendor_indicator,
										  @remarks,
										  @isdeleted,
										  @isdisabled,
										  @status,
										  @created_date,
										  @updated_date,
                                            'NM')";

            var id = await _queryHelper.ExecuteScalar(sql, TakeMetaData(generateLog));

            return generateLog;
        }

        private static readonly Func<IDataReader, string> MakeArchiveAccountNumbers = reader => reader["account_number"].AsString();

        private static readonly Func<IDataReader, string> MakeArchiveCisNumbers = reader => reader["cis_number"].AsString();

        
        private List<IDataParameter> TakeMetaData(GenerateLog log)
        {

            var parameters = new List<IDataParameter>
            {
                QueryHelper.CreateSqlParameter("@id", new Guid(log.Id), SqlDbType.UniqueIdentifier),
                QueryHelper.CreateSqlParameter("@cycle_id",log.CycleID,SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@cust_name",log.Name_1,SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@addr_1",log.Address1,SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@addr_2",log.Address2,SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@addr_3",log.Address3,SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@postcode",log.PostCode,SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@state",log.State,SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@statement_date",log.StatementDate,SqlDbType.DateTime2),
                QueryHelper.CreateSqlParameter("@product_code",log.ProductCode,SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@product_name",log.LProductName,SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@product_name_s",log.ProductName,SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@bank_code",log.LBankCode,SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@bank_code_s",log.BankCode,SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@document_type",log.DocumentType,SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@account_number",log.AccountNumber,SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@cis_number",log.CISNumber,SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@dob",log.DOB,SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@id_number",log.IdNumber,SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@division",log.LDivision,SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@division_s",log.Division,SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@premier",log.LPremier,SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@premier_s",log.Premier,SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@staff",log.LStaff,SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@staff_s",log.Staff,SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@entity",log.LEntity,SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@entity_s",log.Entity,SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@email_address",log.Email,SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@no_of_pages",log.NoOfPages,SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@delivery_method",log.LDeliveryMethod,SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@delivery_method_s",log.DeliveryMethod,SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@doc_id",log.DocId,SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@doc_master_id",log.DocMasterId,SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@doc_instance_id",log.DocInstanceId,SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@vendor_id",log.VendorId,SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@file_name",log.FileName,SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@printing_vendor_indicator",log.PrintingVendorIndicator,SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@doc_type_id",log.DocTypeId,SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@remarks",log.Remarks,SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@isdeleted",log.IsDeleted,SqlDbType.Bit),
                QueryHelper.CreateSqlParameter("@isdisabled",log.IsDisabled,SqlDbType.Bit),
                QueryHelper.CreateSqlParameter("@status",log.Status,SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@created_date",log.CreatedDate,SqlDbType.DateTime2),
                QueryHelper.CreateSqlParameter("@updated_date",log.UpdatedDate,SqlDbType.DateTime2),
            };

            return parameters;
        }

        //private List<IDataParameter> Take(EmatchReport report)
        //{
        //    if (string.IsNullOrEmpty(report.Id))
        //        report.Id = Guid.NewGuid().ToString();

        //    var parameters = new List<IDataParameter>
        //    {

        //        QueryHelper.CreateSqlParameter("@id", new Guid(report.Id), SqlDbType.UniqueIdentifier),
        //        QueryHelper.CreateSqlParameter("@cycle_id", report.CycleId, SqlDbType.NVarChar),
        //        QueryHelper.CreateSqlParameter("@Statement_Date", report.StatementDate, SqlDbType.DateTime2),
        //        QueryHelper.CreateSqlParameter("@File_Name", report.FileName, SqlDbType.NVarChar),
        //        QueryHelper.CreateSqlParameter("@acc_num", report.AccNum, SqlDbType.NVarChar),
        //        QueryHelper.CreateSqlParameter("@product_Name", report.ProductName, SqlDbType.NVarChar),
        //        QueryHelper.CreateSqlParameter("@product_desc", report.ProductDesc, SqlDbType.NVarChar),
        //        QueryHelper.CreateSqlParameter("@statement_type", report.StatementType, SqlDbType.NVarChar),
        //        QueryHelper.CreateSqlParameter("@delivery_method", report.DeliveryMethod, SqlDbType.DateTime2),
        //        QueryHelper.CreateSqlParameter("@account_number", report.AccountNumber, SqlDbType.DateTime2),
        //        QueryHelper.CreateSqlParameter("@cis_number", report.CisStatus, SqlDbType.NVarChar),
        //        QueryHelper.CreateSqlParameter("@customer_name", report.CustomerName, SqlDbType.NVarChar),
        //        QueryHelper.CreateSqlParameter("@address_line1", report.Address1, SqlDbType.NVarChar),
        //        QueryHelper.CreateSqlParameter("@address_line2", report.Address2, SqlDbType.NVarChar),
        //        QueryHelper.CreateSqlParameter("@address_line3", report.Address3, SqlDbType.NVarChar),
        //        QueryHelper.CreateSqlParameter("@postcode", report.PostCode, SqlDbType.NVarChar),
        //        QueryHelper.CreateSqlParameter("@state", report.E1State, SqlDbType.NVarChar),
        //        QueryHelper.CreateSqlParameter("@dob", report.Dob, SqlDbType.NVarChar),
        //        QueryHelper.CreateSqlParameter("@beg_balance", report.BegBalance, SqlDbType.NVarChar),
        //        QueryHelper.CreateSqlParameter("@end_balance", report.EndBalance, SqlDbType.NVarChar),
        //        QueryHelper.CreateSqlParameter("@email_address", report.EmailAddress, SqlDbType.NVarChar),
        //        QueryHelper.CreateSqlParameter("@created_date", report.CreatedDate, SqlDbType.DateTime2),
        //        QueryHelper.CreateSqlParameter("@updated_date", report.UpdatedDate, SqlDbType.DateTime2)

        //    };
        //    return parameters;
        //}

        public async Task<SuppressionReport> SaveSuppressionRecord(SuppressionReport suppressionReport) {

            try
            {
                string sql = $@"INSERT INTO [dbo].[ccss_ln_suppression_report] 
                                        ( [id], 
                                          [cycle_id], 
                                          [statement_date], 
                                          [account_number], 
                                          [cis_number], 
                                          [product_name], 
                                          [product_type], 
                                          [product_code], 
                                          [dob], 
                                          [email_address], 
                                          [delivery_method], 
                                          [processing_stage], 
                                          [status], 
                                          [created_date], 
                                          [updated_date]) 
                                    output inserted.id
                                    VALUES ( @id,
                                            @cycleid,
                                            @statementdate,
                                            @accountnumber,
                                            @cisnumber,
                                            @productname,
                                            @producttype,
                                            @productcode,
                                            @dob,
                                            @emailaddress,
                                            @deliverymethod,
                                            @remarks,
                                            @status,
                                            @created_date,
                                            @updated_date)";

                var id = await _queryHelper.ExecuteScalar(sql, TakeSuppressionReport(suppressionReport));

                return suppressionReport;
            }
            catch(Exception ex)
            {
                Log.Error(ex, "Error while inserting record into suppression " + suppressionReport.Id);
                return null;
            }

        }

        private List<IDataParameter> TakeSuppressionReport(SuppressionReport suppressionReport)
        {

            var parameters = new List<IDataParameter>
            {
                QueryHelper.CreateSqlParameter("@id", Guid.NewGuid(), SqlDbType.UniqueIdentifier),
                QueryHelper.CreateSqlParameter("@cycleid",suppressionReport.CycleID,SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@statementdate",suppressionReport.StatementDate,SqlDbType.Date),
                QueryHelper.CreateSqlParameter("@accountnumber",suppressionReport.AccountNumber,SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@cisnumber",suppressionReport.CISNumber,SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@productcode",suppressionReport.ProductCode,SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@productname",suppressionReport.ProductName,SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@producttype",suppressionReport.ProductType,SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@dob",suppressionReport.Dob,SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@emailaddress",suppressionReport.EmailAddress,SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@deliverymethod",suppressionReport.DeliveryMethod,SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@remarks",suppressionReport.Remarks,SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@status",suppressionReport.Status,SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@created_date",DateTime.Now,SqlDbType.DateTime2),
                QueryHelper.CreateSqlParameter("@updated_date",DateTime.Now,SqlDbType.DateTime2),
            };

            return parameters;
        }

        public async Task<string> UpdateProgressTable(string statusmessage, string cycleid)
        {
            const string sql = @"update [dbo].[ccss_progress] 
                                set 
                                [Status] = @status
                               ,[Date_Updated] =@updatedDate
                                where cycle_id = @cycleid";

            var id = await _queryHelper.ExecuteScalar(sql, progressTake(statusmessage, cycleid));

            return cycleid;
        }

        private static List<IDataParameter> progressTake(string statusmessage, string cycleid)
        {
            
            var parameters = new List<IDataParameter>
            {
                QueryHelper.CreateSqlParameter("@cycleid", cycleid, SqlDbType.NVarChar),
                
                QueryHelper.CreateSqlParameter("@status", statusmessage , SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@updatedDate", DateTime.Now, SqlDbType.DateTime2)
            };

            return parameters;
        }

        public async Task<string> UpdateBatchHistoryTable(string fieldname,string productfullname, string statusmessage, string cycleid)
        {
            string sql = $@"update [dbo].[ccss_batch_run_history] 
                                set 
                                [{fieldname}] = @status
                               ,[updated_date] =@updatedDate
                                where cycle_id = @cycleid";

            var id = await _queryHelper.ExecuteScalar(sql, HistoryTableTake(productfullname, statusmessage, cycleid));

            return cycleid;
        }

        private static List<IDataParameter> HistoryTableTake(string productfullname, string statusmessage, string cycleid)
        {

            var parameters = new List<IDataParameter>
            {
                QueryHelper.CreateSqlParameter("@cycleid", cycleid, SqlDbType.NVarChar),

                QueryHelper.CreateSqlParameter("@status", statusmessage , SqlDbType.NVarChar),
                QueryHelper.CreateSqlParameter("@updatedDate", DateTime.Now, SqlDbType.DateTime2)
            };

            return parameters;
        }

        public Task<List<string>> GetDeliverySuppressAccountNumbersCA(string stmtDate)
        {
            throw new NotImplementedException();
        }

        public Task<List<string>> GetArchiveSuppressAccountNumbersCA(string stmtDate)
        {
            throw new NotImplementedException();
        }

        public Task<bool> LoadCSVFile(string filename, string tableName, List<DbColumn>? columns = null, string? separator = "|")
        {
            throw new NotImplementedException();
        }

        public Task<bool> LoadEmatchCSVFile(string filename, string tableName, List<DbColumn>? columns = null, string? separator = "|")
        {
            throw new NotImplementedException();
        }
    }
}