using FluentValidation;

namespace SmartTaskManagement.Application.Features.Tasks.Validators;

public sealed class UpdateTaskRequestValidator : AbstractValidator<UpdateTaskRequest>
{
    public UpdateTaskRequestValidator()
    {
        RuleFor(request => request.Title)
            .Must(title => !string.IsNullOrWhiteSpace(title))
            .WithMessage("Task title is required.")
            .MaximumLength(200);

        RuleFor(request => request.Description)
            .MaximumLength(2000);

        RuleFor(request => request.AssignedToUserId)
            .Must(userId => !userId.HasValue || userId.Value != Guid.Empty)
            .WithMessage("AssignedToUserId must be a valid user ID.");

        RuleFor(request => request.Status)
            .IsInEnum()
            .WithMessage("Status must be a valid task status.");

        RuleFor(request => request.Priority)
            .IsInEnum()
            .WithMessage("Priority must be a valid task priority.");

        RuleFor(request => request.DueDate)
            .Must(dueDate => !dueDate.HasValue || dueDate.Value != default)
            .WithMessage("DueDate must be a valid date.");
    }
}
