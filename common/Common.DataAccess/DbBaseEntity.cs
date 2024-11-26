using System;

namespace Common.DataAccess
{
    [Serializable]
    public abstract class DbBaseEntity : IEntity<string>
    {
        public string Id { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string UpdatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public bool IsDeleted { get; set; }

       
    }
}
