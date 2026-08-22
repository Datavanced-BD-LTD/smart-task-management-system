using SmartTaskManagement.Domain.Entities;

namespace SmartTaskManagement.Application.Abstractions.Authentication;

public interface ITokenService
{
    AccessTokenResult CreateAccessToken(User user, IReadOnlyCollection<string> roleNames);

    string CreateRefreshToken();

    string HashRefreshToken(string refreshToken);

    DateTime GetRefreshTokenExpiry(DateTime utcNow);
}

public sealed record AccessTokenResult(string Token, DateTime ExpiresAtUtc);
