namespace SmartTaskManagement.Domain.Entities;

public sealed class RefreshToken
{
    private RefreshToken()
    {
    }

    private RefreshToken(
        Guid userId,
        string tokenHash,
        Guid familyId,
        DateTime createdAtUtc,
        DateTime expiresAtUtc)
    {
        RefreshTokenId = Guid.NewGuid();
        UserId = userId;
        TokenHash = tokenHash;
        FamilyId = familyId;
        CreatedAtUtc = createdAtUtc;
        ExpiresAtUtc = expiresAtUtc;
    }

    public Guid RefreshTokenId { get; private set; }

    public Guid UserId { get; private set; }

    public string TokenHash { get; private set; } = string.Empty;

    public Guid FamilyId { get; private set; }

    public DateTime ExpiresAtUtc { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime? RevokedAtUtc { get; private set; }

    public Guid? ReplacedByTokenId { get; private set; }

    public string? CreatedByIp { get; private set; }

    public string? RevokedByIp { get; private set; }

    public string? RevocationReason { get; private set; }

    public User? User { get; private set; }

    public static RefreshToken Create(
        Guid userId,
        string tokenHash,
        Guid familyId,
        DateTime createdAtUtc,
        DateTime expiresAtUtc,
        string? createdByIp)
    {
        var refreshToken = new RefreshToken(
            userId,
            tokenHash,
            familyId,
            createdAtUtc,
            expiresAtUtc)
        {
            CreatedByIp = createdByIp
        };

        return refreshToken;
    }

    public bool IsActive(DateTime utcNow)
    {
        return RevokedAtUtc is null && ExpiresAtUtc > utcNow;
    }

    public void Revoke(
        DateTime revokedAtUtc,
        string reason,
        string? revokedByIp,
        Guid? replacedByTokenId = null)
    {
        if (RevokedAtUtc is not null)
        {
            return;
        }

        RevokedAtUtc = revokedAtUtc;
        RevocationReason = reason;
        RevokedByIp = revokedByIp;
        ReplacedByTokenId = replacedByTokenId;
    }
}
