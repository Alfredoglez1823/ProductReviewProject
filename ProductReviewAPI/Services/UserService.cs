using Microsoft.IdentityModel.Tokens;
using ProductReviewAPI.Models;
using ProductReviewAPI.Repository;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;

namespace ProductReviewAPI.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository<User> _userRepository;
        private readonly IUserRepository<EmailVerification> _email;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<UserService> _logger;

        // Dependencies injected via constructor.
        public UserService(IUserRepository<User> userRepository, IRefreshTokenRepository refreshTokenRepository, IUserRepository<EmailVerification> email, IConfiguration configuration, IHttpContextAccessor httpContextAccessor, ILogger<UserService> logger)
        {
            _userRepository = userRepository;
            _email = email;
            _refreshTokenRepository = refreshTokenRepository;
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        // Registers a new user asynchronously.
        public async Task<User> RegisterUserAsync(User user)
        {
            try
            {
                user.Role = "User";
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(user.PasswordHash);
                await _userRepository.CreateAsync(user);
                _logger.LogInformation($"User {user.Email} registered successfully.");
                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering user.");
                throw;
            }
        }

        // Authenticates a user by email and password.
        public async Task<User> AuthenticateUserAsync(string email, string password)
        {
            if (!IsValidEmailFormat(email))
            {
                _logger.LogWarning($"Authentication failed due to invalid email format: {email}");
                return null;
            }

            try
            {
                var user = await _userRepository.GetByConditionAsync(u => u.Email == email);
                if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
                {
                    _logger.LogWarning($"Authentication failed for user {email}.");
                    return null;
                }

                _logger.LogInformation($"User {email} authenticated successfully.");
                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error authenticating user.");
                throw;
            }
        }

        // Generates JWT and refresh tokens for the user.
        public async Task<(string, string)> GenerateTokens(User user)
        {
            try
            {
                var accessToken = GenerateJwtToken(user, DateTime.UtcNow.AddHours(1));
                var refreshToken = GenerateRefreshToken();

                await SaveRefreshToken(user.Id, refreshToken, DateTime.UtcNow.AddMonths(1));
                _logger.LogInformation($"Tokens generated for user {user.Email}.");

                return (accessToken, refreshToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating tokens.");
                throw;
            }
        }

        // Saves the refresh token in the repository.
        private async Task SaveRefreshToken(int userId, string token, DateTime expiresAt)
        {
            try
            {
                var refreshToken = new RefreshToken
                {
                    UserId = userId,
                    Token = token,
                    ExpiresAt = expiresAt,
                    CreatedAt = DateTime.UtcNow
                };

                await _refreshTokenRepository.AddAsync(refreshToken);
                _logger.LogInformation($"Refresh token saved for user ID {userId}.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving refresh token.");
                throw;
            }
        }

        // Generates a JWT token.
        private string GenerateJwtToken(User user, DateTime expires)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"]);
            var issuer = _configuration["Jwt:Issuer"];
            var audience = _configuration["Jwt:Audience"];

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Role, user.Role)
                }),
                Expires = expires,
                Issuer = issuer,
                Audience = audience,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        // Generates a refresh token.
        private string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomNumber);
                return Convert.ToBase64String(randomNumber);
            }
        }

        // Validates if the refresh token is still valid.
        public async Task<bool> IsRefreshTokenValid(string refreshToken)
        {
            try
            {
                var token = await _refreshTokenRepository.GetByTokenAsync(refreshToken);
                if (token == null || token.ExpiresAt < DateTime.UtcNow)
                {
                    _logger.LogWarning("Invalid or expired refresh token.");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating refresh token.");
                throw;
            }
        }

        // Refreshes the access token using a refresh token.
        public async Task<string> RefreshAccessToken(int userId, string refreshToken)
        {
            try
            {
                var token = await _refreshTokenRepository.GetByTokenAsync(refreshToken);
                if (token.ExpiresAt < DateTime.UtcNow)
                {
                    throw new Exception("Invalid refresh token or token expired.");
                }
                else if(token == null)
                {
                    _logger.LogWarning($"the token does not exist. {userId}");
                    throw new Exception("Invalid refresh token or token expired.");
                }
                else if(token.UserId != userId)
                {
                    _logger.LogWarning($"refresh token: user id in token does not match given. {token.UserId} | {userId}");
                    throw new Exception("Invalid refresh token or token expired.");
                }

                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning($"GetByTokenAsync: User not found. {userId}");
                    throw new Exception("User not found.");
                }

                return GenerateJwtToken(user, DateTime.UtcNow.AddHours(1));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing access token.");
                throw;
            }
        }

        // Creates an email verification entry.
        public async Task<EmailVerification> CreateAsync(EmailVerification entity)
        {
            try
            {
                var result = await _email.CreateAsync(entity);
                _logger.LogInformation($"Email verification code created for {entity.Email}.");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating email verification code.");
                throw;
            }
        }

        // Gets the latest email verification code for a specific email.
        public async Task<EmailVerification> GetLatestCodeAsync(EmailVerification emailVerification)
        {
            if (!IsValidEmailFormat(emailVerification.Email))
            {
                _logger.LogWarning($"Authentication failed due to invalid email format: {emailVerification.Email}");
                return null;
            }
            try
            {
                var result = await _email.GetLatestCodeAsync(emailVerification);
                _logger.LogInformation($"Latest email verification code retrieved for {emailVerification.Email}.");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving latest email verification code.");
                throw;
            }
        }

        public async Task<bool> CheckIfEmailExistsAsync(string email)
        {
            _logger.LogInformation($"Checking if email {email} exists.");
            if (string.IsNullOrEmpty(email))
            {
                _logger.LogError("Email is null or empty.");
                throw new ArgumentException("Email cannot be null or empty.", nameof(email));
            }

            var emailExists = await _userRepository.EmailExistsAsync(email);

            _logger.LogInformation($"Email exists: {emailExists}");

            return emailExists;
        }

        // Validates the email format.
        private bool IsValidEmailFormat(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }
    }
}
