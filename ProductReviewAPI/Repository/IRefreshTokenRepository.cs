using ProductReviewAPI.Models;

namespace ProductReviewAPI.Repository
{
    public interface IRefreshTokenRepository
    {
        Task AddAsync(RefreshToken token);
        Task<RefreshToken> GetByTokenAsync(string token);
    }
}
