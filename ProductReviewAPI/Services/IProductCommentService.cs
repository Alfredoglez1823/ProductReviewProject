using ProductReviewAPI.Models;

namespace ProductReviewAPI.Services
{
    public interface IProductCommentService
    {
        Task<IEnumerable<ProductComment>> GetAllAsync();
        Task<ProductComment?> GetByIdAsync(int id);
        Task<ProductComment> AddAsync(ProductComment productComment);
        Task DeleteAsync(int id);
    }
}
