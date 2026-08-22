namespace SmartTaskManagement.Domain.Entities;

public sealed class Role
{
    private Role()
    {
    }

    public Role(int roleId, string name)
    {
        RoleId = roleId;
        Name = name;
    }

    public int RoleId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public ICollection<UserRole> UserRoles { get; private set; } = new List<UserRole>();
}
