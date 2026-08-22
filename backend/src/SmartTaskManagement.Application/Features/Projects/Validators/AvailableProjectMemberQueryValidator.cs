using FluentValidation;

namespace SmartTaskManagement.Application.Features.Projects.Validators;

public sealed class AvailableProjectMemberQueryValidator
    : AbstractValidator<AvailableProjectMemberQuery>
{
    public AvailableProjectMemberQueryValidator()
    {
        RuleFor(query => query.Keyword)
            .MaximumLength(100)
            .When(query => query.Keyword is not null)
            .WithMessage("Keyword cannot exceed 100 characters.");

        RuleFor(query => query.PageNumber)
            .GreaterThanOrEqualTo(1)
            .WithMessage("PageNumber must be at least 1.");

        RuleFor(query => query.PageSize)
            .InclusiveBetween(1, 100)
            .WithMessage("PageSize must be between 1 and 100.");
    }
}
