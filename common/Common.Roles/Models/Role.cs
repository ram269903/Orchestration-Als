using Common.DataAccess;
using System.Collections.Generic;

namespace Common.Roles.Models
{
    public class Role : DbBaseEntity
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public bool IsSuperRole { get; set; }

        public string GroupId { get; set; }
        public List<ModulePermission> ModulePermissions { get; set; }
        public bool IsEditable { get; set; } = false;
    }

    public class ModulePermission
    {
        public string ModuleId { get; set; }
        public List<string> PermissionIds { get; set; }
        public List<string> PermissionNames { get; set; }
    }

    public class RoleMatrix :DbBaseEntity
    {
        public string RoleName { get; set; }
        public string RoleDescription { get; set; }
        public string ModuleName { get; set; }
        public string AccessPermissions { get; set; }
    }
}
