using FluentValidation;

namespace SmartTaskManagement.Application.Features.Tasks.Validators;

public sealed class UpdateTaskPriorityRequestValidator : AbstractValidator<UpdateTaskPriorityRequest>
{
    public UpdateTaskPriorityRequestValidator()
    {
        RuleFor(request => request.Priority)
            .IsInEnum()
            .WithMessage("Priority must be a valid task priority.");
    }
}
