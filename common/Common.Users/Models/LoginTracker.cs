using Common.DataAccess;
using System;

namespace Common.Users.Models
{
    public class LoginTracker : DbBaseEntity
    {
        public DateTime Date { get; set; }

        public long MaxLoginUsers { get; set; }

    }
}
