using FluentValidation;
using SmartTaskManagement.Application.Abstractions.Ai;
using SmartTaskManagement.Application.Common.Exceptions;

namespace SmartTaskManagement.Application.Features.Ai;

public sealed class AiTaskDescriptionService(
    IAiTaskDescriptionProvider provider,
    IValidator<ImproveTaskDescriptionRequest> validator) : IAiTaskDescriptionService
{
    public async Task<ImproveTaskDescriptionResponse> ImproveAsync(
        ImproveTaskDescriptionRequest request,
        CancellationToken cancellationToken)
    {
        // Validate before invoking the provider so invalid input does not consume
        // model time or count against the AI endpoint's rate limit unnecessarily.
        var validationResult = await validator.ValidateAsync(request, cancellationToken);

        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        // The application service owns the use-case contract; the provider abstraction
        // keeps Ollama-specific transport details out of the API layer.
        var improvedDescription = await provider.ImproveAsync(
            request.Description.Trim(),
            cancellationToken);

        if (string.IsNullOrWhiteSpace(improvedDescription))
        {
            throw new AiProviderResponseException();
        }

        return new ImproveTaskDescriptionResponse(improvedDescription.Trim());
    }
}
