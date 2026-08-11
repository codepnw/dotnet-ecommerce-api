using EcommerceAPI.Application.DTOs.Auth;
using EcommerceAPI.Domain.Shared;

namespace EcommerceAPI.Application.Interfaces.Services;

public interface IAuthService
{
    Task<Result<TokenResponse>> Register(RegisterRequest request);
    Task<Result<TokenResponse>> Login(LoginRequest request);
    Task<Result<TokenResponse>> RefreshToken(RefreshTokenRequest request);

    // OAuth
    Task<Result<TokenResponse>> GoogleLogin(GoogleLoginRequest request);
}