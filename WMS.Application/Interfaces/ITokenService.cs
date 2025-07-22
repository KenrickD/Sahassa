using System.Security.Claims;
using WMS.Domain.Models;

namespace WMS.Application.Interfaces
{
    public interface ITokenService
    {
        Task<string> GenerateAccessTokenAsync(User user);
        Task<RefreshToken> GenerateRefreshTokenAsync(Guid userId, string? ipAddress);
        Task<ClaimsPrincipal?> ValidateAccessTokenAsync(string token, bool validateLifetime = true);
        Task<List<Claim>> GetUserClaimsAsync(User user);
    }
}