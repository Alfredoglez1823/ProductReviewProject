using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProductReviewAPI.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace ProductReviewAPI.Repository
{
    public class UserRepository<T> : IUserRepository<T> where T : class
    {
        private readonly InmueblesDbpContext _context;
        private readonly DbSet<T> _dbSet;
        private readonly ILogger<UserRepository<T>> _logger;

        // Constructor to initialize dependencies.
        public UserRepository(InmueblesDbpContext context, ILogger<UserRepository<T>> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _dbSet = _context.Set<T>();
            //_logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logger = logger;
        }

        // Retrieves an entity by a specified condition.
        public async Task<T> GetByConditionAsync(Expression<Func<T, bool>> expression)
        {
            if (expression == null)
            {
                throw new ArgumentNullException(nameof(expression));
            }

            return await _dbSet.FirstOrDefaultAsync(expression);
        }

        // Retrieves an entity by its ID.
        public async Task<T> GetByIdAsync(int id)
        {
            if (id <= 0)
            {
                throw new ArgumentException("Id must be greater than zero.", nameof(id));
            }

            return await _dbSet.FindAsync(id);
        }

        // Finds entities based on a specified condition.
        public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> expression)
        {
            if (expression == null)
            {
                throw new ArgumentNullException(nameof(expression));
            }

            return await _dbSet.Where(expression).ToListAsync();
        }

        // Updates an existing entity in the database.
        public async Task UpdateAsync(T entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            _dbSet.Update(entity);
            await _context.SaveChangesAsync();
        }

        // Retrieves the latest email verification code for a specific email.
        public async Task<EmailVerification> GetLatestCodeAsync(EmailVerification emailVerification)
        {
            if (emailVerification == null || string.IsNullOrEmpty(emailVerification.Email))
            {
                _logger.LogWarning("GetLatestCodeAsync: emailVerification is null or Email is empty.");
                throw new ArgumentNullException(nameof(emailVerification));
            }

            var propertyInfo = typeof(T).GetProperty("Email");
            if (propertyInfo == null)
            {
                _logger.LogError("GetLatestCodeAsync: Property 'Email' not found in entity.");
                throw new InvalidOperationException("La entidad no tiene la propiedad 'Email'.");
            }

            try
            {
                var entity = await _dbSet.OfType<EmailVerification>()
                    .OrderByDescending(e => e.Id)
                    .FirstOrDefaultAsync(e => e.Email == emailVerification.Email);

                if (entity != null)
                {
                    _logger.LogInformation($"GetLatestCodeAsync: Latest email verification code retrieved for {emailVerification.Email}.");
                }
                else
                {
                    _logger.LogInformation($"GetLatestCodeAsync: No email verification code found for {emailVerification.Email}.");
                }

                return entity;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetLatestCodeAsync: Error retrieving latest email verification code.");
                throw;
            }
        }

        public async Task<bool> EmailExistsAsync(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                throw new ArgumentException($"'{nameof(email)}' cannot be null or empty.", nameof(email));
            }

            //We use parameterized queries to prevent SQL injection
            return await _context.Users.AnyAsync(u => u.Email == email);
        }

        // Creates a new entity in the database.
        public async Task<T> CreateAsync(T entity)
        {
            if (entity == null)
            {
                _logger.LogWarning("CreateAsync: entity is null.");
                throw new ArgumentNullException(nameof(entity));
            }

            try
            {
                _context.Set<T>().Add(entity);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"CreateAsync: Entity of type {typeof(T).Name} created successfully.");
                return entity;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"CreateAsync: Error creating entity of type {typeof(T).Name}.");
                throw;
            }
        }
    }
}
