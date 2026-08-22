using FluentValidation;

namespace SmartTaskManagement.Application.Features.Tasks.Validators;

public sealed class AssignTaskRequestValidator : AbstractValidator<AssignTaskRequest>
{
    public AssignTaskRequestValidator()
    {
        RuleFor(request => request.AssignedUserId)
            .Must(userId => !userId.HasValue || userId.Value != Guid.Empty)
            .WithMessage("AssignedUserId must be a valid user ID when provided.");
    }
}
