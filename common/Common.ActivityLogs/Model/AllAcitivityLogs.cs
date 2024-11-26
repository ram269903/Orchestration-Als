using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.ActivityLogs.Model
{

    public class SupressionActivityLog
    {
        public string ProductName { get; set; }
        public string StatementType { get; set; }
        public DateTime? StatementDate { get; set; }
        public string AccountNumber { get; set; }
        public string CisNumber { get; set; }
        public string ProcessingStage { get; set; }
        public string? Remarks { get; set; }
        public string? Status { get; set; }
    }
    public class StaffActivityLog
    {

        public string Code { get; set; }
        public string Indicator { get; set; }
        public string? Remarks { get; set; }
    }

    public class ResendActivityLog
    {
        public string ProductName { get; set; }
        public string StatementType { get; set; }
        public string StaffTag { get; set; }
        public string AccountNumber { get; set; }
        public string CisNumber { get; set; }
        public string CustomerName { get; set; }
        public string DivisionCode { get; set; }
        public string SegemnetCode { get; set; }
        public string ExistingMail { get; set; }
        public string ResendMail { get; set; }
        public string Status { get; set; }
    }

    public class ProductActivityLog
    {
        public string Application { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string? Remarks { get; set; }
    }

    public class PremiersActivityLog
    {
        public string Code { get; set; }
        public string Description { get; set; }
        public string? Remarks { get; set; }
    }

    public class DivisonAcivityLog
    {
        public string Code { get; set; }
        public string Indicator { get; set; }
        public string? Remarks { get; set; }
        public string Short { get; set; }

    }

    public class DocActivityLog
    {
        public string Name { get; set; }
        public string CustomQuery { get; set; }
        public string VaultDatabase { get; set; }
        public List<string> Permission { get; set; }
        public List<string> ConfigureSearchOptions { get; set; }
    }

    public class UserActivityLog
    {
        public string LoginId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string EmailId { get; set; }

        public string Department { get; set; }
        public string PhoneNumber { get; set; }
        public string RoleName { get; set; }
        public List<string> GroupName { get; set; }
        public string IsActive { get; set; }
        public string Remarks { get; set; }

    }

    public class GroupActivityLog
    {
        public string GroupName { get; set; }
        public string Description { get; set; }
        public List<string> ModuleName { get; set; }
        public string Remarks { get; set; }
    }
    public class CustomQueryActivityLog
    {
        public string Name { get; set; }
        public List<string> Permissions { get; set; }
        public List<string> DatabaseName { get; set; }
        public List<string> TableName { get; set; }
        public List<string> SelctedColumns { get; set; }
        public List<string> Columns { get; set; }
        public string SqlQuery { get; set; }
    }
    public class RoleActivityLog
    {
        public String RoleName { get; set; }
        public string GroupName { get; set; }
        public string Description { get; set; }
        public List<string> ModuleName { get; set; }
        public List<string> PermissionName { get; set; }
    }

    public class DatabaseActivitylog
    {
        public string Name { get; set; }
        public string DataSource { get; set; }
        public string Server { get; set; }
        public string Port { get; set; }
        public string Provider { get; set; }
        public string UserId { get; set; }
        public List<string> Databaseobjects { get; set; }
        public List<string> Group { get; set; }


    }

    public class VaultActivityLog
    {
        public string Name { get; set; }
        public string Server { get; set; }
        public string Port { get; set; }
        public List<string> Database { get; set; }
        public List<string> Group { get; set; }
    }

    public class SearchDocumentStatus
    {
        public string Id { get; set; }

        public string Status { get; set; }
    }

}

