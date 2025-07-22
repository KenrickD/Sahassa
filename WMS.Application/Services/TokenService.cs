using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using WMS.Application.Interfaces;
using WMS.Domain.Models;
using WMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace WMS.Application.Services
{
    public class TokenService : ITokenService
    {
        private readonly AuthSettings _authSettings;
        private readonly AppDbContext _context;

        public TokenService(
            IOptions<AuthSettings> authSettings,
            AppDbContext context)
        {
            _authSettings = authSettings.Value;
            _context = context;
        }

        public async Task<string> GenerateAccessTokenAsync(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_authSettings.JwtSecret);

            var claims = await GetUserClaimsAsync(user);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(_authSettings.AccessTokenExpireMinutes),
                Issuer = _authSettings.JwtIssuer,
                Audience = _authSettings.JwtAudience,
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        public async Task<RefreshToken> GenerateRefreshTokenAsync(Guid userId, string? ipAddress)
        {
            using var rng = new RNGCryptoServiceProvider();
            var randomBytes = new byte[64];
            rng.GetBytes(randomBytes);

            var refreshToken = new RefreshToken
            {
                Id = Guid.NewGuid(),
                Token = Convert.ToBase64String(randomBytes),
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "System",
                ExpiresAt = DateTime.UtcNow.AddDays(_authSettings.RefreshTokenExpireDays),
                CreatedByIp = ipAddress
            };

            _context.RefreshTokens.Add(refreshToken);
            await _context.SaveChangesAsync();

            return refreshToken;
        }

        public async Task<ClaimsPrincipal?> ValidateAccessTokenAsync(string token, bool validateLifetime = true)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(_authSettings.JwtSecret);

                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidIssuer = _authSettings.JwtIssuer,
                    ValidAudience = _authSettings.JwtAudience,
                    ValidateLifetime = validateLifetime,
                    ClockSkew = TimeSpan.Zero
                };

                var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);

                if (validatedToken is JwtSecurityToken jwtToken)
                {
                    var result = jwtToken.Header.Alg.Equals(
                        SecurityAlgorithms.HmacSha256,
                        StringComparison.InvariantCultureIgnoreCase);

                    if (result)
                    {
                        return principal;
                    }
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        public async Task<List<Claim>> GetUserClaimsAsync(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim("FullName", user.FullName),
                new Claim("WarehouseId", user.WarehouseId.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
            };

            if (user.ClientId.HasValue)
            {
                claims.Add(new Claim("ClientId", user.ClientId.Value.ToString()));
            }

            // Add roles
            if (user.UserRoles != null)
            {
                foreach (var userRole in user.UserRoles)
                {
                    claims.Add(new Claim(ClaimTypes.Role, userRole.Role.Name));
                }
            }

            // Add user claims
            if (user.UserClaims != null)
            {
                foreach (var userClaim in user.UserClaims)
                {
                    claims.Add(new Claim(userClaim.ClaimType, userClaim.ClaimValue ?? string.Empty));
                }
            }

            // Add direct permissions
            if (user.UserPermissions != null)
            {
                foreach (var permission in user.UserPermissions)
                {
                    claims.Add(new Claim("Permission", permission.Permission.Name));
                }
            }

            // Add role permissions
            if (user.UserRoles != null)
            {
                var rolePermissions = await _context.RolePermissions
                    .Include(rp => rp.Permission)
                    .Where(rp => user.UserRoles.Select(ur => ur.RoleId).Contains(rp.RoleId))
                    .ToListAsync();

                foreach (var rolePermission in rolePermissions)
                {
                    claims.Add(new Claim("Permission", rolePermission.Permission.Name));
                }
            }

            return claims;
        }
    }
}