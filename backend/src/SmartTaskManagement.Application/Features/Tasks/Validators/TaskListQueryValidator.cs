using FluentValidation;

namespace SmartTaskManagement.Application.Features.Tasks.Validators;

public sealed class TaskListQueryValidator : AbstractValidator<TaskListQuery>
{
    private static readonly string[] AllowedSortColumns =
    [
        "title",
        "status",
        "priority",
        "dueDate",
        "createdAt"
    ];

    private static readonly string[] AllowedSortDirections = ["asc", "desc"];

    public TaskListQueryValidator()
    {
        RuleFor(query => query.Keyword)
            .MaximumLength(200);

        RuleFor(query => query.Status)
            .Must(status => !status.HasValue || Enum.IsDefined(status.Value))
            .WithMessage("Status must be a valid task status.");

        RuleFor(query => query.Priority)
            .Must(priority => !priority.HasValue || Enum.IsDefined(priority.Value))
            .WithMessage("Priority must be a valid task priority.");

        RuleFor(query => query.AssignedUserId)
            .Must(userId => !userId.HasValue || userId.Value != Guid.Empty)
            .WithMessage("AssignedUserId must be a valid user ID.");

        RuleFor(query => query.DueDateFrom)
            .Must(date => !date.HasValue || date.Value != default)
            .WithMessage("DueDateFrom must be a valid date.");

        RuleFor(query => query.DueDateTo)
            .Must(date => !date.HasValue || date.Value != default)
            .WithMessage("DueDateTo must be a valid date.");

        RuleFor(query => query)
            .Must(query =>
                !query.DueDateFrom.HasValue ||
                !query.DueDateTo.HasValue ||
                query.DueDateFrom.Value <= query.DueDateTo.Value)
            .WithMessage("DueDateFrom must be earlier than or equal to DueDateTo.");

        RuleFor(query => query.PageNumber)
            .GreaterThanOrEqualTo(1);

        RuleFor(query => query.PageSize)
            .InclusiveBetween(1, 100);

        RuleFor(query => query.SortColumn)
            .Must(column => AllowedSortColumns.Contains(column, StringComparer.OrdinalIgnoreCase))
            .WithMessage("SortColumn must be one of: title, status, priority, dueDate, createdAt.");

        RuleFor(query => query.SortDirection)
            .Must(direction => AllowedSortDirections.Contains(direction, StringComparer.OrdinalIgnoreCase))
            .WithMessage("SortDirection must be either asc or desc.");
    }
}
