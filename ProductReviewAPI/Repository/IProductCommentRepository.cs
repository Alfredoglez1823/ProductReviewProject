using ProductReviewAPI.Models;

namespace ProductReviewAPI.Repository
{
    public interface IProductCommentRepository
    {
        Task<IEnumerable<ProductComment>> GetAllAsync();
        Task<ProductComment?> GetByIdAsync(int id);
        Task<ProductComment> AddAsync(ProductComment productComment);
        Task DeleteAsync(int id);
    }
}
