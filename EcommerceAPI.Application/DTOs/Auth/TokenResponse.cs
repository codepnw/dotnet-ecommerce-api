namespace EcommerceAPI.Application.DTOs.Auth;

public class TokenResponse
{
    public required string AccessToken { get; set; }
    public required string RefreshToken { get; set; }
    public DateTime AccessTokenExpiry { get; set; }
}
