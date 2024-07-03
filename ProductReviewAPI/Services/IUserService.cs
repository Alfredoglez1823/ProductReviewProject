using ProductReviewAPI.Models;

namespace ProductReviewAPI.Services
{
    public interface IUserService
    {
        Task<User> RegisterUserAsync(User user);
        Task<User> AuthenticateUserAsync(string email, string password);
        Task<(string, string)> GenerateTokens(User user);
        Task<bool> IsRefreshTokenValid(string refreshToken);

        Task<string> RefreshAccessToken(int userId, string refreshToken);

        Task<EmailVerification> CreateAsync(EmailVerification entity);

        Task<EmailVerification> GetLatestCodeAsync(EmailVerification userId);

        Task<bool> CheckIfEmailExistsAsync(string email);
    }
}
