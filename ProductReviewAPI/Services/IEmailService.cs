namespace ProductReviewAPI.Services
{
    public interface IEmailService
    {
        Task<bool> SendEmailAsync(string email, string subject, int message);
    }
}
