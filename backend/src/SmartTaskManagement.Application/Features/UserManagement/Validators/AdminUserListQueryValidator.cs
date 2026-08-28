using FluentValidation;

namespace SmartTaskManagement.Application.Features.UserManagement.Validators;

public sealed class AdminUserListQueryValidator : AbstractValidator<AdminUserListQuery>
{
    public AdminUserListQueryValidator()
    {
        RuleFor(query => query.Keyword)
            .MaximumLength(200);

        RuleFor(query => query.PageNumber)
            .GreaterThanOrEqualTo(1);

        RuleFor(query => query.PageSize)
            .InclusiveBetween(1, 100);
    }
}
