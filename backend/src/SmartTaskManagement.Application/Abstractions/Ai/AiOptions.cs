namespace SmartTaskManagement.Application.Abstractions.Ai;

public sealed class AiOptions
{
    public const string SectionName = "Ai";

    public string Provider { get; set; } = "Ollama";

    public string Endpoint { get; set; } = "http://localhost:11434/api/chat";

    public string Model { get; set; } = "gemma3";

    public string ApiKey { get; set; } = string.Empty;

    public int TimeoutSeconds { get; set; } = 30;
}
