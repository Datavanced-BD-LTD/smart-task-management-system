using FluentValidation;

namespace SmartTaskManagement.Application.Features.Tasks.Validators;

public sealed class UpdateTaskStatusRequestValidator : AbstractValidator<UpdateTaskStatusRequest>
{
    public UpdateTaskStatusRequestValidator()
    {
        RuleFor(request => request.Status)
            .IsInEnum()
            .WithMessage("Status must be a valid task status.");
    }
}
