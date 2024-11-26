using Common.DataAccess;
using System;
using System.Collections.Generic;

namespace Common.Users.Models
{
    public class User : DbBaseEntity
    {
        public string LoginId { get; set; }

        public string? Password { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string EmailId { get; set; }

        public string Department { get; set; }

        public string PhoneNumber { get; set; }

        public string RoleId { get; set; }

        public List<string> Groups { get; set; }

        public bool IsActive { get; set; }

        public DateTime? LastLogin { get; set; }

        public string Token { get; set; }

        public string Remarks { get; set; }
        
		public bool IsSuperUser { get; set; }

        public string Name => $"{FirstName} {LastName}".Trim();

        //public string Name { get; set; }

        public bool IsLogedIn { get; set; }

        public DateTime? StatusUpdatedDate { get; set; }
        public bool? IsLdap { get; set; }


    }
}
