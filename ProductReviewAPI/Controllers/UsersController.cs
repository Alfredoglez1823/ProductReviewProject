using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ProductReviewAPI.Models;
using ProductReviewAPI.Services;
using System.Text.RegularExpressions;

namespace ProductReviewAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        // Declare private fields for the services and logger used in the controller
        private readonly IUserService _userService;
        private readonly IEmailService _emailService;
        private readonly ILogger<UsersController> _logger;

        // Constructor for UsersController to inject necessary services and logger
        public UsersController(IUserService userService, ILogger<UsersController> logger, IEmailService emailService)
        {
            _userService = userService;
            _emailService = emailService;
            _logger = logger;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] CodeRequest email)
        {
            if (string.IsNullOrWhiteSpace(email.Email) || !IsValidEmail(email.Email))
            {
                return BadRequest("Invalid email address.");
            }

            try
            {
                if (await _userService.CheckIfEmailExistsAsync(email.Email))
                    return BadRequest("this email has already been registered");

                int code = GenerateCode();
                var emailVerification = new EmailVerification
                {
                    Email = email.Email,
                    Expiration = DateTime.UtcNow.AddMinutes(10),
                    Code = code
                };

                EmailVerification existingCode = await _userService.GetLatestCodeAsync(emailVerification);

                if (existingCode != null && existingCode.Expiration > DateTime.UtcNow)
                {
                    _logger.LogWarning("Verification code already sent for email: {Email}.", email.Email);
                    return BadRequest("Verification code already sent. Please wait.");
                }

                var saveCode = await _userService.CreateAsync(emailVerification);

                bool emailSent = await _emailService.SendEmailAsync(email.Email, "Code Verification", code);
                if (!emailSent)
                {
                    _logger.LogError("Failed to send verification email to: {Email}.", email.Email);
                    return StatusCode(500, "The email was not sent");
                }

                return Ok("Verification code sent." );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during registration for email: {Email}.", email.Email);
                return StatusCode(500, "Internal server error");
            }
        }


        // Define the EmailVerification method to handle email verification requests via POST
        [HttpPost("emailVerification")]
        public async Task<IActionResult> EmailVerification([FromBody] UserEmailVerificationModel model)
        {
            // Check if the request data for email verification is valid and log a warning if it's not
            if (model.User == null || model.EmailVerification == null)
            {
                _logger.LogWarning("Invalid request data for email verification.");
                return BadRequest("Invalid request data.");
            }

            if(model.User.Email != model.EmailVerification.Email)
            {
                _logger.LogWarning($"EmailVerification: model.user and model.EmailVerification do not match. {model.User.Email} | {model.EmailVerification.Email}");
                return BadRequest("Invalid request data.");
            }

            try
            {
                //Check if the user is already registered
                if (await _userService.CheckIfEmailExistsAsync(model.User.Email))
                    return BadRequest("this email has already been registered");

                EmailVerification storedCode = await _userService.GetLatestCodeAsync(model.EmailVerification);
                if (storedCode == null)
                {
                    _logger.LogWarning($"Verification code does not exist for email: {model.EmailVerification.Email}.");
                    return NotFound("The code does not exist");
                }

                if (storedCode.Code != model.EmailVerification.Code)
                {
                    _logger.LogWarning($"Verification code mismatch for email: {model.EmailVerification.Email}.");
                    return BadRequest("Codes don't match");
                }

                if (storedCode.Expiration < DateTime.UtcNow)
                {
                    _logger.LogWarning($"Verification code expired for email: {model.EmailVerification.Email}.");
                    return BadRequest("The code has expired");
                }

                var createdUser = await _userService.RegisterUserAsync(model.User);
                return Ok(new { Email = createdUser.Email });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error occurred during email verification for email: {model.EmailVerification.Email}.");
                return StatusCode(500, "Internal server error");
            }
        }

        // Define the Login method to handle user login requests via POST
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest loginRequest)
        {
            // Check if the provided email or password is invalid and log a warning if they are
            if (string.IsNullOrWhiteSpace(loginRequest.Email) || string.IsNullOrWhiteSpace(loginRequest.Password))
            {
                _logger.LogWarning("Invalid login attempt with email: {Email}.", loginRequest.Email);
                return BadRequest("Invalid email or password.");
            }
            if (!await _userService.CheckIfEmailExistsAsync(loginRequest.Email))
                return BadRequest("This email has not been registered");

            var user = await _userService.AuthenticateUserAsync(loginRequest.Email, loginRequest.Password);
            if (user == null)
            {
                _logger.LogWarning("Unauthorized login attempt with email: {Email}.", loginRequest.Email);
                return Unauthorized("Invalid email or password.");
            }

            var (accessToken, refreshToken) = await _userService.GenerateTokens(user);

            var loginResponse = new LoginResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken
            };
            return Ok(loginResponse);
        }

        // Define the RefreshToken method to handle token refresh requests via POST
        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest refreshTokenRequest)
        {
            // Check if the refresh token request data is valid and log a warning if it's not
            if (refreshTokenRequest.UserId <= 0 || string.IsNullOrWhiteSpace(refreshTokenRequest.RefreshToken))
            {
                _logger.LogWarning("Invalid refresh token request for user ID: {UserId}.", refreshTokenRequest.UserId);
                return BadRequest("Invalid request data.");
            }

            try
            {
                var newAccessToken = await _userService.RefreshAccessToken(refreshTokenRequest.UserId, refreshTokenRequest.RefreshToken);
                return Ok(newAccessToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Invalid refresh token for user ID: {UserId}.", refreshTokenRequest.UserId);
                return Unauthorized("Invalid refresh token.");
            }
        }

        // Handle OPTIONS requests to provide CORS headers
        [HttpOptions]
        public IActionResult Options()
        {
            Response.Headers.Add("Access-Control-Allow-Origin", "_myAllowSpecificOrigins");
            Response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE");
            Response.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Authorization");
            return Ok();
        }

        // Generate a 6-digit numeric code for email verification
        private static int GenerateCode()
        {
            Random random = new Random();
            int numericCode = random.Next(100000, 999999); // Generates a number between 100000 and 999999
            return numericCode;
        }

        // Validate the provided email address using a regular expression
        private bool IsValidEmail(string email)
        {
            var emailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
            return emailRegex.IsMatch(email);
        }
    }

    public class LoginRequest
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }

    public class CodeRequest
    {
        public string Email { get; set; }
    }

    public class RefreshTokenRequest
    {
        public int UserId { get; set; }
        public string RefreshToken { get; set; }
    }
}

public class UserEmailVerificationModel
{
    public User User { get; set; }
    public EmailVerification EmailVerification { get; set; }
}

public class LoginResponse
{
    public string AccessToken { get; set; }
    public string RefreshToken { get; set; }
}