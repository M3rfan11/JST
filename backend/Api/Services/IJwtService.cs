using Api.DTOs;
using Api.Models;

namespace Api.Services;

public interface IJwtService
{
    string GenerateAccessToken(User user, List<string> roles);
    string GenerateRefreshToken();
    bool ValidateRefreshToken(string refreshToken);
    Task<LoginResponse?> LoginAsync(LoginRequest request);
    Task<LoginResponse?> RefreshTokenAsync(RefreshTokenRequest request);
}
