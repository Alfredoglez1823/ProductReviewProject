using ProductReviewAPI.Models;
using System.Linq.Expressions;

namespace ProductReviewAPI.Repository
{
    public interface IUserRepository<T> where T : class
    {
        Task<T> GetByConditionAsync(Expression<Func<T, bool>> expression);
        Task<T> GetByIdAsync(int id);
        Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> expression);

        Task<EmailVerification> GetLatestCodeAsync(EmailVerification emailVerification);

        Task UpdateAsync(T entity);

        Task<bool> EmailExistsAsync(string email);
        Task<T> CreateAsync(T entity);
    }
}
