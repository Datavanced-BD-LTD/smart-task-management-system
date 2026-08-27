using FluentValidation;

namespace SmartTaskManagement.Application.Features.Projects.Validators;

public sealed class ProjectListQueryValidator : AbstractValidator<ProjectListQuery>
{
    private static readonly string[] AllowedSortFields = ["name", "createdAt", "updatedAt"];
    private static readonly string[] AllowedSortDirections = ["asc", "desc"];

    public ProjectListQueryValidator()
    {
        // Project list inputs are validated before the store builds its IQueryable,
        // keeping sorting and pagination predictable for every caller.
        RuleFor(query => query.Search)
            .MaximumLength(200);

        RuleFor(query => query.Page)
            .GreaterThanOrEqualTo(1);

        RuleFor(query => query.PageSize)
            .InclusiveBetween(1, 100);

        RuleFor(query => query.SortBy)
            .Must(sortBy => AllowedSortFields.Contains(sortBy, StringComparer.OrdinalIgnoreCase))
            .WithMessage("SortBy must be one of: name, createdAt, updatedAt.");

        RuleFor(query => query.SortDirection)
            .Must(direction => AllowedSortDirections.Contains(direction, StringComparer.OrdinalIgnoreCase))
            .WithMessage("SortDirection must be either asc or desc.");
    }
}
