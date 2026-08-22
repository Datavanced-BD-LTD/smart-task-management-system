namespace SmartTaskManagement.Domain.Entities;

public sealed class ProjectMember
{
    private ProjectMember()
    {
    }

    public ProjectMember(
        Guid projectId,
        Guid userId,
        Guid addedByUserId,
        DateTime addedAtUtc)
    {
        ProjectId = projectId;
        UserId = userId;
        AddedByUserId = addedByUserId;
        AddedAtUtc = addedAtUtc;
    }

    public Guid ProjectId { get; private set; }

    public Guid UserId { get; private set; }

    public Guid AddedByUserId { get; private set; }

    public DateTime AddedAtUtc { get; private set; }

    public Project? Project { get; private set; }

    public User? User { get; private set; }

    public User? AddedByUser { get; private set; }
}
