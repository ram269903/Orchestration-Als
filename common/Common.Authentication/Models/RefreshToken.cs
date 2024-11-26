using Common.DataAccess;

namespace Common.Authentication.Models
{
    public class RefreshToken : DbBaseEntity
    {
        public string LoginId { get; set; }
        public string Refreshtoken { get; set; }
        public bool Revoked { get; set; }
    }
}
