using Common.ActiveDirectory;
using Common.Config;
using Common.Users.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;

namespace Common.Authentication.PgSql
{
    public class AuthService : IAuthService
    {
        private readonly AppSettings _appSettings;
        private readonly Users.PgSql.UserService _usersRepository;
        private readonly ILogger _logger;

        public AuthService(IOptions<DbConfig> dbConfig, IOptions<AppSettings> appSettings, ILoggerFactory loggerFactory)
        {
            if (loggerFactory != null)
                _logger = loggerFactory.CreateLogger("AuthService");

            var databaseConfig = dbConfig.Value;

            _appSettings = appSettings.Value;
            _usersRepository = new Users.PgSql.UserService(databaseConfig);
        }

        public AuthService(DbConfig dbConfig, AppSettings appSettings)
        {
            var databaseConfig = dbConfig;

            _appSettings = appSettings;
            _usersRepository = new Users.PgSql.UserService(databaseConfig);
        }

        public async Task<User> Authenticate(string loginId, string password)
        {
            var isAuthorized = false;
            User loginUser = null;

            if (!string.IsNullOrEmpty(loginId) && !string.IsNullOrEmpty(password) && password.Trim() != string.Empty)
            {
                loginUser = await _usersRepository.GetUserByLoginId(loginId.ToLower());

                if (loginUser == null) return null;

                if (_appSettings.IsLdapAuthentication)
                {
                    isAuthorized = new LdapService(_appSettings.LdapSettings,_logger).Authenticate(loginId, password);
                }
                else
                {
                    if (loginUser != null)
                    {
                        isAuthorized = loginUser.Password == SecurityManager.GenerateHash(password) && loginUser.IsActive == true;
                    }
                }
            }

            if (isAuthorized)
            {
                //loginUser.Token = SecurityManager.GenerateToken(_appSettings.JwtToken.Secret, _appSettings.JwtToken.TokenExpiryMinutes, loginUser.LoginId);

                //loginUser.Password = null;

                return loginUser;
            }
            else
                return null;
        }

        public async Task<User> RefreshToken(string refreshToken)
        {
            //var rToken = await _refreshTokensRepository.GetOneAsync(m => m.Refreshtoken == refreshToken);

            //if (rToken == null)
            //{
            //    return null;
            //}

            //var token = GenerateToken(_appSettings.JwtToken.Secret, _appSettings.JwtToken.TokenExpiryMinutes, rToken.LoginId);


            //rToken.Refreshtoken = Guid.NewGuid().ToString();
            //await _refreshTokensRepository.UpdateOneAsync(rToken);


            //return Ok(new { token = token, refToken = rToken.Refreshtoken });

            return null;
        }
    }
}
