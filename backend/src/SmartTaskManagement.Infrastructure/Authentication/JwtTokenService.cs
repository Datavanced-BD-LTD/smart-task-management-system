using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SmartTaskManagement.Application.Abstractions.Authentication;
using SmartTaskManagement.Application.Abstractions.Common;
using SmartTaskManagement.Domain.Entities;

namespace SmartTaskManagement.Infrastructure.Authentication;

public sealed class JwtTokenService(
    IOptions<JwtOptions> jwtOptions,
    ISystemClock systemClock) : ITokenService
{
    private readonly JwtOptions _options = jwtOptions.Value;

    public AccessTokenResult CreateAccessToken(
        User user,
        IReadOnlyCollection<string> roleNames)
    {
        var now = systemClock.UtcNow;
        var expiresAtUtc = now.AddMinutes(_options.AccessTokenMinutes);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, $"{user.FirstName} {user.LastName}"),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        claims.AddRange(roleNames.Select(roleName => new Claim(ClaimTypes.Role, roleName)));

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        var signingCredentials = new SigningCredentials(
            securityKey,
            SecurityAlgorithms.HmacSha256);
        var securityToken = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: now,
            expires: expiresAtUtc,
            signingCredentials: signingCredentials);

        return new AccessTokenResult(
            new JwtSecurityTokenHandler().WriteToken(securityToken),
            expiresAtUtc);
    }

    public string CreateRefreshToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
    }

    public string HashRefreshToken(string refreshToken)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)));
    }

    public DateTime GetRefreshTokenExpiry(DateTime utcNow)
    {
        return utcNow.AddDays(_options.RefreshTokenDays);
    }
}
