using ProductReviewAPI.Models;
using ProductReviewAPI.Repository;

namespace ProductReviewAPI.Services
{
    public class ProductCommentService : IProductCommentService
    {
        private readonly IProductCommentRepository _repository;

        public ProductCommentService(IProductCommentRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<ProductComment>> GetAllAsync()
        {
            return await _repository.GetAllAsync();
        }

        public async Task<ProductComment?> GetByIdAsync(int id)
        {
            return await _repository.GetByIdAsync(id);
        }

        public async Task<ProductComment> AddAsync(ProductComment productComment)
        {
            return await _repository.AddAsync(productComment);
        }

        public async Task DeleteAsync(int id)
        {
            await _repository.DeleteAsync(id);
        }
    }
}
