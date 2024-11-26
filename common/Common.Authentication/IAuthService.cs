using Common.Users.Models;
using System.Threading.Tasks;

namespace Common.Authentication
{
    public interface IAuthService
    {
        Task<User> Authenticate(string loginId, string password);
        Task<User> RefreshToken(string refreshToken);
    }
}
