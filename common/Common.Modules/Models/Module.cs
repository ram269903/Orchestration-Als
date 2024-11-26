using Common.DataAccess;
using System.Collections.Generic;

namespace Common.Modules.Models
{
    public class Module : DbBaseEntity
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public List<Feature> Features { get; set; }
        public List<Permission> Permissions { get; set; }
        public string ParentId { get; set; }
    }

    public class Feature
    {
        public string Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }

    public class Permission
    {
        public string Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }
}
