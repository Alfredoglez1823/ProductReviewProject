using Microsoft.EntityFrameworkCore;
using ProductReviewAPI.Models;
using System;
using System.Threading.Tasks;

namespace ProductReviewAPI.Repository
{
    public class RefreshTokenRepository : IRefreshTokenRepository
    {
        private readonly InmueblesDbpContext _context;

        // Constructor to initialize dependencies.
        public RefreshTokenRepository(InmueblesDbpContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        // Adds a new refresh token to the database.
        public async Task AddAsync(RefreshToken token)
        {
            if (token == null)
            {
                throw new ArgumentNullException(nameof(token));
            }

            await _context.RefreshTokens.AddAsync(token);
            await _context.SaveChangesAsync();
        }

        // Retrieves a refresh token by its token string.
        public async Task<RefreshToken> GetByTokenAsync(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                throw new ArgumentException("Token cannot be null or empty.", nameof(token));
            }

            return await _context.RefreshTokens.FirstOrDefaultAsync(t => t.Token == token);
        }
    }
}
