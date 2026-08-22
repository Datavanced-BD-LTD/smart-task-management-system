namespace SmartTaskManagement.Infrastructure.Authentication;

public sealed class SeedAdminOptions
{
    public const string SectionName = "Authentication:SeedAdmin";

    public string? Email { get; set; }

    public string? Password { get; set; }

    public string FirstName { get; set; } = "System";

    public string LastName { get; set; } = "Administrator";
}
