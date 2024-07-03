using Microsoft.EntityFrameworkCore;
using ProductReviewAPI.Models;

namespace ProductReviewAPI.Repository
{
    public class ProductCommentRepository : IProductCommentRepository
    {
        private readonly InmueblesDbpContext _context;

        public ProductCommentRepository(InmueblesDbpContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<ProductComment>> GetAllAsync()
        {
            return await _context.ProductComments.ToListAsync();
        }

        public async Task<ProductComment?> GetByIdAsync(int id)
        {
            return await _context.ProductComments.FindAsync(id);
        }

        public async Task<ProductComment> AddAsync(ProductComment productComment)
        {
            _context.ProductComments.Add(productComment);
            await _context.SaveChangesAsync();
            return productComment;
        }

        public async Task DeleteAsync(int id)
        {
            var productComment = await _context.ProductComments.FindAsync(id);
            if (productComment != null)
            {
                _context.ProductComments.Remove(productComment);
                await _context.SaveChangesAsync();
            }
        }
    }
}
