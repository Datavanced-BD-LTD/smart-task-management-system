namespace SmartTaskManagement.Domain.Entities;

public sealed class UserRole
{
    private UserRole()
    {
    }

    public UserRole(Guid userId, int roleId)
    {
        UserId = userId;
        RoleId = roleId;
    }

    public Guid UserId { get; private set; }

    public int RoleId { get; private set; }

    public User? User { get; set; }

    public Role? Role { get; set; }
}
