namespace SmartTaskManagement.Domain.Entities;

public sealed class Project
{
    private Project()
    {
    }

    public Project(
        string name,
        string? description,
        Guid projectManagerId,
        Guid createdByUserId,
        DateTime createdAtUtc)
    {
        ProjectId = Guid.NewGuid();
        Name = name.Trim();
        Description = description?.Trim();
        ProjectManagerId = projectManagerId;
        CreatedByUserId = createdByUserId;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    public Guid ProjectId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public Guid ProjectManagerId { get; private set; }

    public Guid CreatedByUserId { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public bool IsDeleted { get; private set; }

    public DateTime? DeletedAtUtc { get; private set; }

    public Guid? DeletedByUserId { get; private set; }

    public byte[] RowVersion { get; private set; } = [];

    public User? ProjectManager { get; private set; }

    public User? CreatedByUser { get; private set; }

    public ICollection<ProjectMember> ProjectMembers { get; private set; } = new List<ProjectMember>();

    public ICollection<TaskItem> TaskItems { get; private set; } = new List<TaskItem>();

    public void UpdateDetails(
        string name,
        string? description,
        Guid projectManagerId,
        DateTime updatedAtUtc)
    {
        Name = name.Trim();
        Description = description?.Trim();
        ProjectManagerId = projectManagerId;
        UpdatedAtUtc = updatedAtUtc;
    }

    public void SoftDelete(Guid deletedByUserId, DateTime deletedAtUtc)
    {
        if (IsDeleted)
        {
            return;
        }

        IsDeleted = true;
        DeletedAtUtc = deletedAtUtc;
        DeletedByUserId = deletedByUserId;
        UpdatedAtUtc = deletedAtUtc;
    }
}
