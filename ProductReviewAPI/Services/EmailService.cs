using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ProductReviewAPI.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        // Dependencies injected via constructor.
        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        // Sends an email asynchronously.
        public async Task<bool> SendEmailAsync(string email, string subject, int message)
        {
            try
            {
                string fromEmail = _configuration["EmailSettings:FromEmail"];
                string fromPassword = _configuration["EmailSettings:FromPassword"];

                if (string.IsNullOrWhiteSpace(fromEmail) || string.IsNullOrWhiteSpace(fromPassword))
                {
                    _logger.LogError("Email settings are not configured correctly.");
                    return false;
                }

                MailMessage mail = new MailMessage();
                mail.From = new MailAddress(fromEmail);
                mail.Subject = subject;
                mail.To.Add(new MailAddress(email));
                mail.Body = message.ToString();
                mail.IsBodyHtml = true;

                var smtpClient = new SmtpClient(_configuration["EmailSettings:SmtpHost"])
                {
                    Port = int.Parse(_configuration["EmailSettings:Port"]),
                    Credentials = new NetworkCredential(fromEmail, fromPassword),
                    EnableSsl = true,
                };

                await smtpClient.SendMailAsync(mail);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error sending mail: {ex.Message}");
                return false;
            }
        }
    }
}
