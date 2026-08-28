using FluentValidation;

namespace SmartTaskManagement.Application.Features.UserManagement.Validators;

public sealed class UpdateManagedUserRequestValidator
    : AbstractValidator<UpdateManagedUserRequest>
{
    public UpdateManagedUserRequestValidator()
    {
        RuleFor(request => request.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(256);

        RuleFor(request => request.FirstName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(request => request.LastName)
            .NotEmpty()
            .MaximumLength(100);
    }
}
