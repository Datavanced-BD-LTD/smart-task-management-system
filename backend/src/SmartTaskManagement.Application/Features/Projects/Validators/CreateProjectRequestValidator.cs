using FluentValidation;

namespace SmartTaskManagement.Application.Features.Projects.Validators;

public sealed class CreateProjectRequestValidator : AbstractValidator<CreateProjectRequest>
{
    public CreateProjectRequestValidator()
    {
        RuleFor(request => request.Name)
            .Must(name => !string.IsNullOrWhiteSpace(name))
            .WithMessage("Project name is required.")
            .MaximumLength(200);

        RuleFor(request => request.Description)
            .MaximumLength(2000);
    }
}
