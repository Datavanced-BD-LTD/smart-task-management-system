using Microsoft.AspNetCore.Identity;
using SmartTaskManagement.Application.Abstractions.Authentication;
using SmartTaskManagement.Domain.Entities;

namespace SmartTaskManagement.Infrastructure.Authentication;

public sealed class AspNetPasswordService : IPasswordService
{
    private readonly PasswordHasher<User> _passwordHasher = new();

    public string HashPassword(User user, string password)
    {
        return _passwordHasher.HashPassword(user, password);
    }

    public bool VerifyPassword(User user, string password, string passwordHash)
    {
        var result = _passwordHasher.VerifyHashedPassword(user, passwordHash, password);

        return result is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;
    }
}
