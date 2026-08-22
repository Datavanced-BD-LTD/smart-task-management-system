namespace SmartTaskManagement.Application.Common.Exceptions;

public sealed class AiProviderTimeoutException()
    : Exception("The AI provider timed out.");

public sealed class AiProviderUnavailableException()
    : Exception("The AI provider is unavailable.");

public sealed class AiProviderResponseException()
    : Exception("The AI provider returned an invalid response.");
