namespace SmartTaskManagement.Domain.Entities;

public sealed class User
{
    private User()
    {
    }

    public User(string email, string firstName, string lastName)
    {
        UserId = Guid.NewGuid();
        Email = email.Trim().ToLowerInvariant();
        NormalizedEmail = Email.ToUpperInvariant();
        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        CreatedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = CreatedAtUtc;
        IsActive = true;
    }

    public Guid UserId { get; private set; }

    public string Email { get; private set; } = string.Empty;

    public string NormalizedEmail { get; private set; } = string.Empty;

    public string PasswordHash { get; private set; } = string.Empty;

    public string FirstName { get; private set; } = string.Empty;

    public string LastName { get; private set; } = string.Empty;

    public bool IsActive { get; private set; }

    public DateTime? LastLoginAtUtc { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public byte[] RowVersion { get; private set; } = [];

    public ICollection<UserRole> UserRoles { get; private set; } = new List<UserRole>();

    public ICollection<RefreshToken> RefreshTokens { get; private set; } = new List<RefreshToken>();

    public void SetPasswordHash(string passwordHash)
    {
        PasswordHash = passwordHash;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void AssignRole(Role role)
    {
        if (UserRoles.All(userRole => userRole.RoleId != role.RoleId))
        {
            UserRoles.Add(new UserRole(UserId, role.RoleId)
            {
                Role = role
            });
        }
    }

    public void ReplaceRoles(Role role, DateTime updatedAtUtc)
    {
        UserRoles.Clear();
        AssignRole(role);
        UpdatedAtUtc = updatedAtUtc;
    }

    public void RecordSuccessfulLogin()
    {
        LastLoginAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = LastLoginAtUtc.Value;
    }
}
