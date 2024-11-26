using System;

namespace Common.DataAccess
{
    public interface IEntity<T>
    {
        T Id { get; set; }
        string CreatedBy { get; set; }
        DateTime? CreatedDate { get; set; }
        string UpdatedBy { get; set; }
        DateTime? UpdatedDate { get; set; }
    }
}
