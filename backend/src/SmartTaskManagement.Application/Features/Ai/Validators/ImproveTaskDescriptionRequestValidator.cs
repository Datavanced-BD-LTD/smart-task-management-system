using FluentValidation;

namespace SmartTaskManagement.Application.Features.Ai.Validators;

public sealed class ImproveTaskDescriptionRequestValidator
    : AbstractValidator<ImproveTaskDescriptionRequest>
{
    public ImproveTaskDescriptionRequestValidator()
    {
        RuleFor(request => request.Description)
            .Cascade(CascadeMode.Stop)
            .Must(description => !string.IsNullOrWhiteSpace(description))
            .WithMessage("Description is required.")
            .MinimumLength(5)
            .WithMessage("Description must be at least 5 characters long.")
            .MaximumLength(2000)
            .WithMessage("Description cannot exceed 2000 characters.");
    }
}
