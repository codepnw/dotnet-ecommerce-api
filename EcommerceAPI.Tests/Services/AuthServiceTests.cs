using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using EcommerceAPI.Application.Commons.Constrants;
using EcommerceAPI.Application.DTOs.Auth;
using EcommerceAPI.Application.Interfaces.Repositories;
using EcommerceAPI.Application.Services;
using EcommerceAPI.Domain.Entities;
using EcommerceAPI.Domain.Shared;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace EcommerceAPI.Tests.Services;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<IOAuthService> _oauthServiceMock = new();
    private readonly IConfiguration _config;
    private readonly AuthService _authService;

    private const string MockEmail = "testuser@example.com";
    private const string MockPassword = "TestPassword123!";

    public AuthServiceTests()
    {
        var configData = new Dictionary<string, string?>
        {
            ["Jwt:Key"] = "SuperSecretKeyThatIsAtLeast32BytesLong!",
            ["Jwt:Issuer"] = "EcommerceAPI.Api",
            ["Jwt:Audience"] = "AuthAPIClients",
            ["Jwt:AccessTokenExpiryMinutes"] = "15",
            ["Jwt:RefreshTokenExpiryDays"] = "7"
        };

        _config = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        _authService = new AuthService(
            _userRepositoryMock.Object,
            _config,
            NullLogger<AuthService>.Instance,
            _oauthServiceMock.Object
        );
    }

    #region Register Tests

    [Fact]
    public async Task Register_Success()
    {
        // Arrange
        var request = new RegisterRequest { Email = MockEmail, Password = MockPassword };
        _userRepositoryMock.Setup(x => x.ExistsByEmailAsync(request.Email)).ReturnsAsync(false);
        _userRepositoryMock.Setup(x => x.CreateAsync(It.IsAny<User>()))
            .ReturnsAsync((User user) => user);

        // Act
        var result = await _authService.Register(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.AccessToken.Should().NotBeNullOrEmpty();
        result.Data.RefreshToken.Should().NotBeNullOrEmpty();

        _userRepositoryMock.Verify(x => x.CreateAsync(It.Is<User>(u => u.Email == request.Email)), Times.Once);
        _userRepositoryMock.Verify(x => x.UpdateAsync(It.Is<User>(u => u.Email == request.Email && u.RefreshToken == result.Data.RefreshToken)), Times.Once);
    }

    [Fact]
    public async Task Register_Success_GenerateJwtWithCorrectClaims()
    {
        // Arrange
        var request = new RegisterRequest { Email = MockEmail, Password = MockPassword };
        _userRepositoryMock.Setup(x => x.ExistsByEmailAsync(request.Email)).ReturnsAsync(false);
        _userRepositoryMock.Setup(x => x.CreateAsync(It.IsAny<User>()))
            .ReturnsAsync((User user) => user);

        // Act
        var result = await _authService.Register(request);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(result.Data!.AccessToken);

        jwtToken.Issuer.Should().Be("EcommerceAPI.Api");
        jwtToken.Audiences.Should().Contain("AuthAPIClients");

        var claims = jwtToken.Claims.ToList();
        claims.Should().Contain(c => c.Type == ClaimTypes.Email && c.Value == MockEmail);
        claims.Should().Contain(c => c.Type == ClaimTypes.Role && c.Value == UserRoles.User);
    }

    [Fact]
    public async Task Register_Fail_DuplicateEmail()
    {
        // Arrange
        var request = new RegisterRequest { Email = MockEmail, Password = MockPassword };
        _userRepositoryMock.Setup(x => x.ExistsByEmailAsync(request.Email)).ReturnsAsync(true);

        // Act
        var result = await _authService.Register(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Email already exists");
        result.ErrorCode.Should().Be(ErrorCode.Conflict);

        _userRepositoryMock.Verify(x => x.CreateAsync(It.IsAny<User>()), Times.Never);
        _userRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<User>()), Times.Never);
    }

    #endregion

    #region Login Tests

    [Fact]
    public async Task Login_Success()
    {
        // Arrange
        var request = new LoginRequest { Email = MockEmail, Password = MockPassword };
        var existingUser = new User
        {
            Email = MockEmail,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(MockPassword),
            Role = UserRoles.User
        };

        _userRepositoryMock.Setup(x => x.GetByEmailAsync(request.Email)).ReturnsAsync(existingUser);

        // Act
        var result = await _authService.Login(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.AccessToken.Should().NotBeNullOrEmpty();
        result.Data.RefreshToken.Should().NotBeNullOrEmpty();

        existingUser.LastLoginAt.Should().NotBeNull();
        existingUser.RefreshToken.Should().Be(result.Data.RefreshToken);

        _userRepositoryMock.Verify(x => x.UpdateAsync(existingUser), Times.Once);
    }

    [Fact]
    public async Task Login_Fail_InvalidEmail()
    {
        // Arrange
        var request = new LoginRequest { Email = "nonexistent@test.com", Password = MockPassword };
        _userRepositoryMock.Setup(x => x.GetByEmailAsync(request.Email)).ReturnsAsync((User?)null);

        // Act
        var result = await _authService.Login(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Invalid email or password");
        result.ErrorCode.Should().Be(ErrorCode.BadRequest);

        _userRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task Login_Fail_WrongPassword()
    {
        // Arrange
        var request = new LoginRequest { Email = MockEmail, Password = "WrongPassword123!" };
        var existingUser = new User
        {
            Email = MockEmail,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(MockPassword),
            Role = UserRoles.User
        };

        _userRepositoryMock.Setup(x => x.GetByEmailAsync(request.Email)).ReturnsAsync(existingUser);

        // Act
        var result = await _authService.Login(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Invalid email or password");
        result.ErrorCode.Should().Be(ErrorCode.BadRequest);

        _userRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<User>()), Times.Never);
    }

    #endregion

    #region RefreshToken Tests

    [Fact]
    public async Task RefreshToken_Success()
    {
        // Arrange
        const string oldRefreshToken = "valid-refresh-token";
        var request = new RefreshTokenRequest { RefreshToken = oldRefreshToken };
        var existingUser = new User
        {
            Email = MockEmail,
            RefreshToken = oldRefreshToken,
            RefreshTokenExpiry = DateTime.UtcNow.AddDays(7),
            Role = UserRoles.User
        };

        _userRepositoryMock.Setup(x => x.GetByRefreshToken(oldRefreshToken)).ReturnsAsync(existingUser);

        // Act
        var result = await _authService.RefreshToken(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.AccessToken.Should().NotBeNullOrEmpty();
        result.Data.RefreshToken.Should().NotBeNullOrEmpty();
        result.Data.RefreshToken.Should().NotBe(oldRefreshToken);

        existingUser.RefreshToken.Should().Be(result.Data.RefreshToken);

        _userRepositoryMock.Verify(x => x.UpdateAsync(existingUser), Times.Once);
    }

    [Fact]
    public async Task RefreshToken_Fail_InvalidToken()
    {
        // Arrange
        var request = new RefreshTokenRequest { RefreshToken = "invalid-token" };
        _userRepositoryMock.Setup(x => x.GetByRefreshToken(request.RefreshToken)).ReturnsAsync((User?)null);

        // Act
        var result = await _authService.RefreshToken(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Invalid refresh token");
        result.ErrorCode.Should().Be(ErrorCode.BadRequest);

        _userRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task RefreshToken_Fail_Expired()
    {
        // Arrange
        const string expiredRefreshToken = "expired-refresh-token";
        var request = new RefreshTokenRequest { RefreshToken = expiredRefreshToken };
        var existingUser = new User
        {
            Email = MockEmail,
            RefreshToken = expiredRefreshToken,
            RefreshTokenExpiry = DateTime.UtcNow.AddDays(-1),
            Role = UserRoles.User
        };

        _userRepositoryMock.Setup(x => x.GetByRefreshToken(expiredRefreshToken)).ReturnsAsync(existingUser);

        // Act
        var result = await _authService.RefreshToken(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Refresh token expired");
        result.ErrorCode.Should().Be(ErrorCode.BadRequest);

        _userRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<User>()), Times.Never);
    }

    #endregion

    #region GoogleLogin Tests

    [Fact]
    public async Task GoogleLogin_Success_ExistingUserByGoogleId()
    {
        // Arrange
        const string idToken = "valid-google-id-token";
        var request = new GoogleLoginRequest { IdToken = idToken };
        var googleUser = new GoogleLoginResponse
        {
            GoogleId = "google-user-id-123",
            Email = "googleuser@example.com",
            DisplayName = "Google User",
            PictureUrl = "http://example.com/pic.jpg",
            Verified = true
        };

        var existingUser = new User
        {
            GoogleId = "google-user-id-123",
            Email = "googleuser@example.com",
            Role = UserRoles.User
        };

        _oauthServiceMock.Setup(x => x.GoogleVerifyToken(idToken)).ReturnsAsync(googleUser);
        _userRepositoryMock.Setup(x => x.GetByGoogleIdAsync(idToken)).ReturnsAsync(existingUser);

        // Act
        var result = await _authService.GoogleLogin(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.AccessToken.Should().NotBeNullOrEmpty();
        result.Data.RefreshToken.Should().NotBeNullOrEmpty();

        existingUser.LastLoginAt.Should().NotBeNull();
        existingUser.RefreshToken.Should().Be(result.Data.RefreshToken);

        _userRepositoryMock.Verify(x => x.UpdateAsync(existingUser), Times.Once);
    }

    [Fact]
    public async Task GoogleLogin_Success_LinkExistingLocalUserByEmail()
    {
        // Arrange
        const string idToken = "valid-google-id-token";
        var request = new GoogleLoginRequest { IdToken = idToken };
        var googleUser = new GoogleLoginResponse
        {
            GoogleId = "google-user-id-123",
            Email = MockEmail,
            DisplayName = "Google Display Name",
            PictureUrl = "http://example.com/pic.jpg",
            Verified = true
        };

        var existingLocalUser = new User
        {
            Email = MockEmail,
            GoogleId = null,
            Role = UserRoles.User
        };

        _oauthServiceMock.Setup(x => x.GoogleVerifyToken(idToken)).ReturnsAsync(googleUser);
        _userRepositoryMock.Setup(x => x.GetByGoogleIdAsync(idToken)).ReturnsAsync((User?)null);
        _userRepositoryMock.Setup(x => x.GetByEmailAsync(googleUser.Email)).ReturnsAsync(existingLocalUser);

        // Act
        var result = await _authService.GoogleLogin(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        existingLocalUser.GoogleId.Should().Be(googleUser.GoogleId);
        existingLocalUser.PictureUrl.Should().Be(googleUser.PictureUrl);
        existingLocalUser.DisplayName.Should().Be(googleUser.DisplayName);

        _userRepositoryMock.Verify(x => x.UpdateAsync(existingLocalUser), Times.Once);
    }

    [Fact]
    public async Task GoogleLogin_Fail_EmailAlreadyLinkedToDifferentGoogleAccount()
    {
        // Arrange
        const string idToken = "valid-google-id-token";
        var request = new GoogleLoginRequest { IdToken = idToken };
        var googleUser = new GoogleLoginResponse
        {
            GoogleId = "google-user-id-123",
            Email = MockEmail,
            Verified = true
        };

        var existingUserLinkedToOtherAccount = new User
        {
            Email = MockEmail,
            GoogleId = "existing-different-google-id",
            Role = UserRoles.User
        };

        _oauthServiceMock.Setup(x => x.GoogleVerifyToken(idToken)).ReturnsAsync(googleUser);
        _userRepositoryMock.Setup(x => x.GetByGoogleIdAsync(idToken)).ReturnsAsync((User?)null);
        _userRepositoryMock.Setup(x => x.GetByEmailAsync(googleUser.Email)).ReturnsAsync(existingUserLinkedToOtherAccount);

        // Act
        var result = await _authService.GoogleLogin(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Email already linked to another account");
        result.ErrorCode.Should().Be(ErrorCode.Conflict);

        _userRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task GoogleLogin_Success_CreateNewUser()
    {
        // Arrange
        const string idToken = "valid-google-id-token";
        var request = new GoogleLoginRequest { IdToken = idToken };
        var googleUser = new GoogleLoginResponse
        {
            GoogleId = "google-user-id-123",
            Email = "newgoogleuser@example.com",
            DisplayName = "New Google User",
            PictureUrl = "http://example.com/pic.jpg",
            Verified = true
        };

        _oauthServiceMock.Setup(x => x.GoogleVerifyToken(idToken)).ReturnsAsync(googleUser);
        _userRepositoryMock.Setup(x => x.GetByGoogleIdAsync(idToken)).ReturnsAsync((User?)null);
        _userRepositoryMock.Setup(x => x.GetByEmailAsync(googleUser.Email)).ReturnsAsync((User?)null);
        _userRepositoryMock.Setup(x => x.CreateAsync(It.IsAny<User>()))
            .ReturnsAsync((User user) => user);

        // Act
        var result = await _authService.GoogleLogin(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.AccessToken.Should().NotBeNullOrEmpty();

        _userRepositoryMock.Verify(x => x.CreateAsync(It.Is<User>(u =>
            u.Email == googleUser.Email &&
            u.GoogleId == googleUser.GoogleId &&
            u.DisplayName == googleUser.DisplayName &&
            u.PictureUrl == googleUser.PictureUrl &&
            u.Role == UserRoles.User
        )), Times.Once);

        _userRepositoryMock.Verify(x => x.UpdateAsync(It.Is<User>(u => u.Email == googleUser.Email)), Times.Once);
    }

    [Fact]
    public async Task GoogleLogin_Fail_InvalidToken()
    {
        // Arrange
        const string idToken = "invalid-token";
        var request = new GoogleLoginRequest { IdToken = idToken };
        _oauthServiceMock.Setup(x => x.GoogleVerifyToken(idToken)).ReturnsAsync((GoogleLoginResponse?)null);

        // Act
        var result = await _authService.GoogleLogin(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Invalid google token");
        result.ErrorCode.Should().Be(ErrorCode.BadRequest);

        _userRepositoryMock.Verify(x => x.CreateAsync(It.IsAny<User>()), Times.Never);
        _userRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<User>()), Times.Never);
    }

    #endregion
}