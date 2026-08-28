using FluentValidation;
using SmartTaskManagement.Domain.Constants;

namespace SmartTaskManagement.Application.Features.UserManagement.Validators;

public sealed class UpdateManagedUserRoleRequestValidator
    : AbstractValidator<UpdateManagedUserRoleRequest>
{
    public UpdateManagedUserRoleRequestValidator()
    {
        RuleFor(request => request.Role)
            .Must(role =>
                string.Equals(role?.Trim(), RoleNames.ProjectManager, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(role?.Trim(), RoleNames.TeamMember, StringComparison.OrdinalIgnoreCase))
            .WithMessage("Role must be ProjectManager or TeamMember.");
    }
}
