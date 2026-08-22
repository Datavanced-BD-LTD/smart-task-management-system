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
        var validationResult = await validator.ValidateAsync(request, cancellationToken);

        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

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
