using Common.DataAccess;
using System.Collections.Generic;

namespace Common.Groups.Models
{
    public class Group : DbBaseEntity
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public bool IsSuperGroup { get; set; } = false;
        public List<ModuleFeature> ModuleFeatures { get; set; }
        public List<string> Modules { get; set; }

        public bool IsEditable { get; set; } = false;

    }

    public class ModuleFeature
    {
        public string ModuleId { get; set; }
        public string ModuleName { get; set; }
        public List<string> FeatureIds { get; set; }
    }
}
