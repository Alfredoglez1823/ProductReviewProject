

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ProductReviewAPI.Controllers;
using ProductReviewAPI.Models;
using ProductReviewAPI.Services;

namespace ProductReviewUnitTesting
{
    public class UsersControllerTests
    {
        private readonly Mock<IUserService> _mockUserService;
        private readonly Mock<IEmailService> _mockEmailService;
        private readonly Mock<ILogger<UsersController>> _mockLogger;
        private readonly UsersController _controller;

        public UsersControllerTests()
        {
            _mockUserService = new Mock<IUserService>();
            _mockEmailService = new Mock<IEmailService>();
            _mockLogger = new Mock<ILogger<UsersController>>();
            _controller = new UsersController(_mockUserService.Object, _mockLogger.Object, _mockEmailService.Object);
        }

        [Fact]
        public async Task Register_ReturnsBadRequest_WhenEmailIsInvalid()
        {
            // Arrange
            var emailRequest = new CodeRequest { Email = "invalid-email" };

            // Act
            var result = await _controller.Register(emailRequest);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Invalid email address.", badRequestResult.Value);
        }

        [Fact]
        public async Task Register_ReturnsBadRequest_WhenEmailAlreadyExists()
        {
            // Arrange
            var emailRequest = new CodeRequest { Email = "test@example.com" };
            _mockUserService.Setup(s => s.CheckIfEmailExistsAsync(emailRequest.Email)).ReturnsAsync(true);

            // Act
            var result = await _controller.Register(emailRequest);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("this email has already been registered", badRequestResult.Value);
        }

        [Fact]
        public async Task Register_ReturnsOk_WhenEmailIsValidAndNotRegistered()
        {
            // Arrange
            var emailRequest = new CodeRequest { Email = "test@example.com" };
            _mockUserService.Setup(s => s.CheckIfEmailExistsAsync(emailRequest.Email)).ReturnsAsync(false);
            _mockEmailService.Setup(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>())).ReturnsAsync(true);
            _mockUserService.Setup(s => s.GetLatestCodeAsync(It.IsAny<EmailVerification>())).ReturnsAsync((EmailVerification)null);
            _mockUserService.Setup(s => s.CreateAsync(It.IsAny<EmailVerification>())).ReturnsAsync(new EmailVerification());

            // Act
            var result = await _controller.Register(emailRequest);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            dynamic value = okResult.Value;
            Assert.Equal("Verification code sent.", okResult.Value);
        }


        [Fact]
        public async Task EmailVerification_ReturnsBadRequest_WhenRequestDataIsInvalid()
        {
            // Arrange
            var model = new UserEmailVerificationModel { User = null, EmailVerification = null };

            // Act
            var result = await _controller.EmailVerification(model);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Invalid request data.", badRequestResult.Value);
        }

        [Fact]
        public async Task EmailVerification_ReturnsBadRequest_WhenEmailsDoNotMatch()
        {
            // Arrange
            var model = new UserEmailVerificationModel
            {
                User = new User { Email = "test1@example.com" },
                EmailVerification = new EmailVerification { Email = "test2@example.com", Code = 123456 }
            };

            // Act
            var result = await _controller.EmailVerification(model);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Invalid request data.", badRequestResult.Value);
        }

        [Fact]
        public async Task EmailVerification_ReturnsNotFound_WhenCodeDoesNotExist()
        {
            // Arrange
            var model = new UserEmailVerificationModel
            {
                User = new User { Email = "test@example.com" },
                EmailVerification = new EmailVerification { Email = "test@example.com", Code = 123456 }
            };
            _mockUserService.Setup(s => s.GetLatestCodeAsync(It.IsAny<EmailVerification>())).ReturnsAsync((EmailVerification)null);

            // Act
            var result = await _controller.EmailVerification(model);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("The code does not exist", notFoundResult.Value);
        }

        [Fact]
        public async Task Login_ReturnsBadRequest_WhenEmailOrPasswordIsInvalid()
        {
            // Arrange
            var loginRequest = new LoginRequest { Email = "", Password = "" };

            // Act
            var result = await _controller.Login(loginRequest);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Invalid email or password.", badRequestResult.Value);
        }

        [Fact]
        public async Task Login_ReturnsUnauthorized_WhenCredentialsAreInvalid()
        {
            // Arrange
            var loginRequest = new LoginRequest { Email = "test@example.com", Password = "password" };
            _mockUserService.Setup(s => s.AuthenticateUserAsync(loginRequest.Email, loginRequest.Password)).ReturnsAsync((User)null);

            // Act
            var result = await _controller.Login(loginRequest);

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Equal("Invalid email or password.", unauthorizedResult.Value);
        }

        [Fact]
        public async Task Login_ReturnsOk_WhenCredentialsAreValid()
        {
            // Arrange
            var loginRequest = new LoginRequest { Email = "test@example.com", Password = "password" };
            var user = new User { Id = 1, Email = loginRequest.Email, Role = "User" };
            _mockUserService.Setup(s => s.AuthenticateUserAsync(loginRequest.Email, loginRequest.Password)).ReturnsAsync(user);
            _mockUserService.Setup(s => s.GenerateTokens(user)).ReturnsAsync(("accessToken", "refreshToken"));

            // Act
            var result = await _controller.Login(loginRequest);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var loginResponse = Assert.IsType<LoginResponse>(okResult.Value); // Deserializar a LoginResponse
            Assert.NotNull(loginResponse);
            Assert.Equal("accessToken", loginResponse.AccessToken);
            Assert.Equal("refreshToken", loginResponse.RefreshToken);
        }


        [Fact]
        public async Task RefreshToken_ReturnsBadRequest_WhenRequestDataIsInvalid()
        {
            // Arrange
            var refreshTokenRequest = new RefreshTokenRequest { UserId = 0, RefreshToken = "" };

            // Act
            var result = await _controller.RefreshToken(refreshTokenRequest);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Invalid request data.", badRequestResult.Value);
        }

        [Fact]
        public async Task RefreshToken_ReturnsUnauthorized_WhenRefreshTokenIsInvalid()
        {
            // Arrange
            var refreshTokenRequest = new RefreshTokenRequest { UserId = 1, RefreshToken = "invalidToken" };
            _mockUserService.Setup(s => s.RefreshAccessToken(refreshTokenRequest.UserId, refreshTokenRequest.RefreshToken))
                .ThrowsAsync(new Exception("Invalid refresh token or token expired."));

            // Act
            var result = await _controller.RefreshToken(refreshTokenRequest);

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Equal("Invalid refresh token.", unauthorizedResult.Value);
        }


        [Fact]
        public async Task RefreshToken_ReturnsOk_WhenRefreshTokenIsValid()
        {
            // Arrange
            var refreshTokenRequest = new RefreshTokenRequest { UserId = 1, RefreshToken = "validToken" };
            _mockUserService.Setup(s => s.RefreshAccessToken(refreshTokenRequest.UserId, refreshTokenRequest.RefreshToken))
                .ReturnsAsync("newAccessToken");

            // Act
            var result = await _controller.RefreshToken(refreshTokenRequest);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal("newAccessToken", okResult.Value);
        }
    }
}