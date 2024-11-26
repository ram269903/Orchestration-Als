using Common.ActiveDirectory;
using Common.Authentication.Models;
using Common.Config;
using Common.DataAccess.Mongo;
using Common.Users.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;

namespace Common.Authentication.Mongo
{
    public class AuthService : IAuthService
    {
        private readonly IRepository<RefreshToken> _refreshTokensRepository;
        private readonly IRepository<User> _usersRepository;
        private readonly AppSettings _appSettings;
        private const string RefreshTokensRepository = "RefreshTokens";
        private const string UsersRepository = "Users";
        private readonly ILogger _logger;


        public AuthService(IOptions<DbConfig> dbConfig, IOptions<AppSettings> appSettings, ILoggerFactory loggerFactory)
        {
            if (loggerFactory != null)
                _logger = loggerFactory.CreateLogger("AuthService");

            var databaseConfig = dbConfig.Value;

            var dbSettings = new DbSettings { ConnectionString = databaseConfig.ConnectionString, Database = databaseConfig.Database };

            _refreshTokensRepository = new MongoRepository<RefreshToken>(dbSettings, RefreshTokensRepository);
            _usersRepository = new MongoRepository<User>(dbSettings, UsersRepository);
            _appSettings = appSettings.Value;
        }

        public AuthService(DbConfig dbConfig, AppSettings appSettings)
        {
            var databaseConfig = dbConfig;

            var dbSettings = new DbSettings { ConnectionString = databaseConfig.ConnectionString, Database = databaseConfig.Database };

            _refreshTokensRepository = new MongoRepository<RefreshToken>(dbSettings, RefreshTokensRepository);
            _usersRepository = new MongoRepository<User>(dbSettings, UsersRepository);
            _appSettings = appSettings;
        }

        public async Task<User> Authenticate(string loginId, string password)
        {
            var isAuthorized = false;
            User loginUser = null;

            if (!string.IsNullOrEmpty(loginId) && !string.IsNullOrEmpty(password) && password.Trim() != string.Empty)
            {
                loginUser = await _usersRepository.GetOneAsync(x => x.LoginId.ToLower() == loginId.ToLower());

                if (loginUser == null) return null;

                if (_appSettings.IsLdapAuthentication)
                {
                    isAuthorized = new LdapService(_appSettings.LdapSettings, _logger).Authenticate(loginId, password);
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
            var rToken = await _refreshTokensRepository.GetOneAsync(m => m.Refreshtoken == refreshToken);

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
