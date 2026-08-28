using FluentValidation;
using SmartTaskManagement.Domain.Constants;

namespace SmartTaskManagement.Application.Features.UserManagement.Validators;

public sealed class CreateManagedUserRequestValidator
    : AbstractValidator<CreateManagedUserRequest>
{
    public CreateManagedUserRequestValidator()
    {
        RuleFor(request => request.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(256);

        RuleFor(request => request.Password)
            .NotEmpty()
            .MinimumLength(8)
            .MaximumLength(128)
            .Matches("[A-Z]").WithMessage("Password must contain an uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain a lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain a digit.")
            .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain a special character.");

        RuleFor(request => request.FirstName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(request => request.LastName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(request => request.Role)
            .Must(IsManagedRole)
            .WithMessage("Role must be ProjectManager or TeamMember.");
    }

    private static bool IsManagedRole(string role)
    {
        return string.Equals(role?.Trim(), RoleNames.ProjectManager, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(role?.Trim(), RoleNames.TeamMember, StringComparison.OrdinalIgnoreCase);
    }
}
