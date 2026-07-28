using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using EcommerceAPI.Application.Commons;
using EcommerceAPI.Application.Commons.Constrants;
using EcommerceAPI.Application.DTOs.Auth;
using EcommerceAPI.Application.Interfaces;
using EcommerceAPI.Application.Interfaces.Repositories;
using EcommerceAPI.Domain.Entities;
using EcommerceAPI.Domain.Shared;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace EcommerceAPI.Application.Services;

public interface IAuthService
{
    Task<Result<TokenResponse>> Register(RegisterRequest request);
    Task<Result<TokenResponse>> Login(LoginRequest request);
    Task<Result<TokenResponse>> RefreshToken(RefreshTokenRequest request);

    // OAuth
    Task<Result<TokenResponse>> GoogleLogin(GoogleLoginRequest request);
}

public class AuthService(
    IUserRepository userRepository,
    IConfiguration config,
    ILogger<AuthService> logger,
    IOAuthService oauthService
) : IAuthService
{
    public async Task<Result<TokenResponse>> Register(RegisterRequest request)
    {
        if (await userRepository.ExistsByEmailAsync(request.Email))
        {
            logger.LogWarning("Register Failed: {Email} already exists", request.Email);
            return Result<TokenResponse>.Failure("Email already exists", ErrorCode.Conflict);
        }

        var user = await userRepository.CreateAsync(new User
        {
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
        });
        
        // Generate Token Response
        var tokenResponse = GenerateTokenResponse(user);

        user.RefreshToken = tokenResponse.RefreshToken;
        user.RefreshTokenExpiry = GetRefreshTokenExpiry();
        await userRepository.UpdateAsync(user);

        return Result<TokenResponse>.Success(tokenResponse);
    }

    public async Task<Result<TokenResponse>> Login(LoginRequest request)
    {
        // Find Email
        var user = await userRepository.GetByEmailAsync(request.Email);
        if (user is null)
        {
            logger.LogWarning("Login Failed: Email: {Email} not found", request.Email);
            return Result<TokenResponse>.Failure("Invalid email or password", ErrorCode.BadRequest);
        }

        // Verify Password
        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            logger.LogWarning("Login Failed: Email: {Email} invalid password", request.Email);
            return Result<TokenResponse>.Failure("Invalid email or password", ErrorCode.BadRequest);
        }

        // Generate Token Response
        var tokenResponse = GenerateTokenResponse(user);

        user.LastLoginAt = DateTime.UtcNow;
        user.RefreshToken = tokenResponse.RefreshToken;
        user.RefreshTokenExpiry = GetRefreshTokenExpiry();
        await userRepository.UpdateAsync(user);

        return Result<TokenResponse>.Success(tokenResponse);
    }

    public async Task<Result<TokenResponse>> RefreshToken(RefreshTokenRequest request)
    {
        // Find Refresh Token
        var user = await userRepository.GetByRefreshToken(request.RefreshToken);
        if (user is null)
        {
            logger.LogWarning("Refresh Token Failed: {Token} invalid", request.RefreshToken);
            return Result<TokenResponse>.Failure("Invalid refresh token", ErrorCode.BadRequest);
        }

        // Check Expiry
        if (user.RefreshTokenExpiry < DateTime.UtcNow)
        {
            logger.LogWarning("Refresh Token Failed: {Token} expired", request.RefreshToken);
            return Result<TokenResponse>.Failure("Refresh token expired", ErrorCode.BadRequest);
        }

        // Generate Token Response
        var tokenResponse = GenerateTokenResponse(user);

        user.RefreshToken = tokenResponse.RefreshToken;
        user.RefreshTokenExpiry = GetRefreshTokenExpiry();
        await userRepository.UpdateAsync(user);

        return Result<TokenResponse>.Success(tokenResponse);
    }

    public async Task<Result<TokenResponse>> GoogleLogin(GoogleLoginRequest request)
    {
        var userInfo = await oauthService.GoogleVerifyToken(request.IdToken);
        if (userInfo == null)
        {
            logger.LogWarning("Google login failed: invalid token");
            return Result<TokenResponse>.Failure("Invalid google token", ErrorCode.BadRequest);
        }

        logger.LogInformation("Google login email: {Email}", userInfo.Email);
        
        // Find from GoogleId
        var user = await userRepository.GetByGoogleIdAsync(request.IdToken);

        if (user != null)
        {
            // Found User
            logger.LogInformation("Existing google user: {UserId}", user.Id);
        }
        else
        {
            // Find from Email
            // user = await context.Users.FirstOrDefaultAsync(u => u.Email == userInfo.Email);
            user = await userRepository.GetByEmailAsync(userInfo.Email);

            if (user != null)
            {
                // Local Account -> Link Google Account
                if (user.GoogleId != null)
                {
                    logger.LogWarning("Email {Email} already linked to different Google account", userInfo.Email);
                    return Result<TokenResponse>.Failure("Email already linked to another account", ErrorCode.Conflict);
                }

                user.GoogleId = userInfo.GoogleId;
                user.PictureUrl = userInfo.PictureUrl;
                user.DisplayName = userInfo.DisplayName;
                logger.LogInformation("Link Google account to existing user: {UserId}", user.Id);
            }
            else
            {
                // New User
                user = new User
                {
                    Email = userInfo.Email,
                    GoogleId = userInfo.GoogleId,
                    DisplayName = userInfo.DisplayName,
                    PictureUrl = userInfo.PictureUrl,
                    Role = UserRoles.User,
                    PasswordHash = ""
                };

                // context.Users.Add(user);
                await userRepository.CreateAsync(user);
                logger.LogInformation("Created new user from Google login: {Email}", userInfo.Email);
            }
        }
        
        // Generate Token Response
        var response = GenerateTokenResponse(user);

        // Update Data
        user.LastLoginAt = DateTime.UtcNow;
        user.RefreshToken = response.RefreshToken;
        user.RefreshTokenExpiry = GetRefreshTokenExpiry();
        await userRepository.UpdateAsync(user);

        return Result<TokenResponse>.Success(response);
    }

    // =========================== PRIVATE ==============================

    private TokenResponse GenerateTokenResponse(User user)
    {
        var accessToken = GetAccessToken(user);
        var refreshToken = GetRefreshToken();

        return new TokenResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            AccessTokenExpiry = GetAccessTokenExpiry()
        };
    }

    private string GetAccessToken(User user)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role),
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: config["Jwt:Issuer"],
            audience: config["Jwt:Audience"],
            claims: claims,
            expires: GetAccessTokenExpiry(),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string GetRefreshToken()
    {
        var randomNumber = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);

        return Convert.ToBase64String(randomNumber);
    }

    private DateTime GetAccessTokenExpiry()
    {
        return DateTime.UtcNow.AddMinutes(double.Parse(config["Jwt:AccessTokenExpiryMinutes"]!));
    }

    private DateTime GetRefreshTokenExpiry()
    {
        return DateTime.UtcNow.AddDays(double.Parse(config["Jwt:RefreshTokenExpiryDays"]!));
    }
}