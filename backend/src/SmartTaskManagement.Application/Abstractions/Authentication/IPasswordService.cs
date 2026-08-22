using SmartTaskManagement.Domain.Entities;

namespace SmartTaskManagement.Application.Abstractions.Authentication;

public interface IPasswordService
{
    string HashPassword(User user, string password);

    bool VerifyPassword(User user, string password, string passwordHash);
}
