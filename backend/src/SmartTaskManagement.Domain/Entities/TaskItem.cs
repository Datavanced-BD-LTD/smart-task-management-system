using SmartTaskManagement.Domain.Enums;
using SmartTaskManagement.Domain.Policies;
using TaskStatusEnum = SmartTaskManagement.Domain.Enums.TaskStatus;

namespace SmartTaskManagement.Domain.Entities;

public sealed class TaskItem
{
    private TaskItem()
    {
    }

    public TaskItem(
        Guid projectId,
        string title,
        string? description,
        Guid? assignedToUserId,
        Guid createdByUserId,
        TaskStatusEnum status,
        TaskPriority priority,
        DateTime? dueDate,
        DateTime createdAtUtc)
    {
        Id = Guid.NewGuid();
        ProjectId = projectId;
        Title = title.Trim();
        Description = description?.Trim();
        AssignedToUserId = assignedToUserId;
        CreatedByUserId = createdByUserId;
        Status = status;
        Priority = priority;
        DueDate = dueDate;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }

    public Guid ProjectId { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public Guid? AssignedToUserId { get; private set; }

    public Guid CreatedByUserId { get; private set; }

    public TaskStatusEnum Status { get; private set; }

    public TaskPriority Priority { get; private set; }

    public DateTime? DueDate { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime? UpdatedAtUtc { get; private set; }

    public Project? Project { get; private set; }

    public User? AssignedToUser { get; private set; }

    public User? CreatedByUser { get; private set; }

    public void AssignTo(Guid? assignedToUserId, DateTime updatedAtUtc)
    {
        // Null is a deliberate value: it represents an unassigned task rather than
        // requiring a special endpoint or placeholder user record.
        AssignedToUserId = assignedToUserId;
        UpdatedAtUtc = updatedAtUtc;
    }

    public void ChangeStatus(TaskStatusEnum status, DateTime updatedAtUtc)
    {
        // The entity delegates workflow rules to a domain policy so every caller,
        // including the full update path, receives the same transition validation.
        TaskStatusTransitionPolicy.EnsureAllowed(Status, status);
        Status = status;
        UpdatedAtUtc = updatedAtUtc;
    }

    public void ChangePriority(TaskPriority priority, DateTime updatedAtUtc)
    {
        Priority = priority;
        UpdatedAtUtc = updatedAtUtc;
    }

    public void UpdateDetails(
        string title,
        string? description,
        Guid? assignedToUserId,
        TaskStatusEnum status,
        TaskPriority priority,
        DateTime? dueDate,
        DateTime updatedAtUtc)
    {
        // Domain mutation methods keep timestamps and transition rules together so
        // application services cannot accidentally persist an inconsistent state.
        TaskStatusTransitionPolicy.EnsureAllowed(Status, status);
        Title = title.Trim();
        Description = description?.Trim();
        AssignedToUserId = assignedToUserId;
        Status = status;
        Priority = priority;
        DueDate = dueDate;
        UpdatedAtUtc = updatedAtUtc;
    }
}
