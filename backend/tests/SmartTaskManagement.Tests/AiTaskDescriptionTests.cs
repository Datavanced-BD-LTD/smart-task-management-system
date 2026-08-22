using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartTaskManagement.Application.Abstractions.Ai;
using SmartTaskManagement.Application.Common.Exceptions;
using SmartTaskManagement.Application.Features.Ai;
using SmartTaskManagement.Application.Features.Ai.Validators;
using SmartTaskManagement.Infrastructure.Ai;
using Xunit;

namespace SmartTaskManagement.Tests;

public sealed class AiTaskDescriptionTests
{
    [Fact]
    public async Task Short_description_is_improved_by_the_fake_provider()
    {
        var provider = new FakeAiTaskDescriptionProvider(
            "Design and implement a responsive login page with validation and clear error feedback.");
        var service = CreateService(provider);

        var result = await service.ImproveAsync(
            new ImproveTaskDescriptionRequest("  make login page  "),
            CancellationToken.None);

        Assert.Equal(
            "Design and implement a responsive login page with validation and clear error feedback.",
            result.ImprovedDescription);
        Assert.Equal("make login page", provider.LastDescription);
    }

    [Fact]
    public async Task Grammar_and_clarity_prompt_is_sent_correctly()
    {
        string? requestBody = null;
        var handler = new StubHttpMessageHandler(async (request, cancellationToken) =>
        {
            requestBody = await request.Content!.ReadAsStringAsync(cancellationToken);
            return JsonResponse("Improved task description.");
        });
        var provider = CreateOllamaProvider(handler);

        var result = await provider.ImproveAsync(
            "make login page",
            CancellationToken.None);

        Assert.Equal("Improved task description.", result);
        using var json = JsonDocument.Parse(requestBody!);
        var messages = json.RootElement.GetProperty("messages");
        var systemPrompt = messages[0].GetProperty("content").GetString();
        var userPrompt = messages[1].GetProperty("content").GetString();

        Assert.Equal("system", messages[0].GetProperty("role").GetString());
        Assert.Equal("user", messages[1].GetProperty("role").GetString());
        Assert.Contains("grammar", systemPrompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("clarity", systemPrompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("professional", systemPrompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("actionable", systemPrompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("make login page", userPrompt, StringComparison.Ordinal);
        Assert.Contains("<task-description>", userPrompt, StringComparison.Ordinal);
        Assert.False(json.RootElement.GetProperty("stream").GetBoolean());
        Assert.Equal(
            0.2,
            json.RootElement.GetProperty("options").GetProperty("temperature").GetDouble());
    }

    [Theory]
    [InlineData("")]
    [InlineData("    ")]
    [InlineData("abcd")]
    public async Task Invalid_input_is_rejected(string description)
    {
        var provider = new FakeAiTaskDescriptionProvider("should not be returned");
        var service = CreateService(provider);

        await Assert.ThrowsAsync<FluentValidation.ValidationException>(() => service.ImproveAsync(
            new ImproveTaskDescriptionRequest(description),
            CancellationToken.None));

        Assert.Null(provider.LastDescription);
    }

    [Fact]
    public async Task Description_longer_than_two_thousand_characters_is_rejected()
    {
        var provider = new FakeAiTaskDescriptionProvider("should not be returned");
        var service = CreateService(provider);

        await Assert.ThrowsAsync<FluentValidation.ValidationException>(() => service.ImproveAsync(
            new ImproveTaskDescriptionRequest(new string('a', 2001)),
            CancellationToken.None));

        Assert.Null(provider.LastDescription);
    }

    [Fact]
    public async Task Provider_timeout_is_handled_safely()
    {
        var handler = new StubHttpMessageHandler((_, cancellationToken) =>
            Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken)
                .ContinueWith<HttpResponseMessage>(
                    _ => new HttpResponseMessage(HttpStatusCode.OK),
                    CancellationToken.None,
                    TaskContinuationOptions.OnlyOnRanToCompletion,
                    TaskScheduler.Default));
        var provider = CreateOllamaProvider(
            handler,
            new AiOptions { TimeoutSeconds = 1 });

        await Assert.ThrowsAsync<AiProviderTimeoutException>(() => provider.ImproveAsync(
            "make login page",
            CancellationToken.None));
    }

    [Fact]
    public async Task Provider_failure_returns_a_safe_error_without_the_api_key()
    {
        const string apiKey = "test-api-key-that-must-not-escape";
        var logger = new RecordingLogger<OllamaAiTaskDescriptionProvider>();
        var handler = new StubHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
            {
                Content = new StringContent($"provider failure: {apiKey}")
            }));
        var provider = CreateOllamaProvider(
            handler,
            new AiOptions { ApiKey = apiKey },
            logger);

        var exception = await Assert.ThrowsAsync<AiProviderUnavailableException>(() => provider.ImproveAsync(
            "make login page",
            CancellationToken.None));

        Assert.DoesNotContain(apiKey, exception.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(apiKey, string.Join(Environment.NewLine, logger.Messages), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Invalid_provider_response_is_handled_safely()
    {
        var handler = new StubHttpMessageHandler((_, _) =>
            Task.FromResult(JsonResponse(string.Empty)));
        var provider = CreateOllamaProvider(handler);

        await Assert.ThrowsAsync<AiProviderResponseException>(() => provider.ImproveAsync(
            "make login page",
            CancellationToken.None));
    }

    private static AiTaskDescriptionService CreateService(
        IAiTaskDescriptionProvider provider)
    {
        return new AiTaskDescriptionService(
            provider,
            new ImproveTaskDescriptionRequestValidator());
    }

    private static OllamaAiTaskDescriptionProvider CreateOllamaProvider(
        HttpMessageHandler handler,
        AiOptions? options = null,
        RecordingLogger<OllamaAiTaskDescriptionProvider>? logger = null)
    {
        var httpClient = new HttpClient(handler);
        return new OllamaAiTaskDescriptionProvider(
            new TestHttpClientFactory(httpClient),
            Options.Create(options ?? new AiOptions
            {
                Endpoint = "http://localhost:11434/api/chat",
                Model = "gemma3",
                TimeoutSeconds = 30
            }),
            logger ?? new RecordingLogger<OllamaAiTaskDescriptionProvider>());
    }

    private static HttpResponseMessage JsonResponse(string content)
    {
        var json = $$"""
            {
              "message": {
                "role": "assistant",
                "content": "{{content}}"
              }
            }
            """;

        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }

    private sealed class FakeAiTaskDescriptionProvider(string result)
        : IAiTaskDescriptionProvider
    {
        public string? LastDescription { get; private set; }

        public Task<string> ImproveAsync(
            string description,
            CancellationToken cancellationToken)
        {
            LastDescription = description;
            return Task.FromResult(result);
        }
    }

    private sealed class TestHttpClientFactory(HttpClient httpClient)
        : IHttpClientFactory
    {
        public HttpClient CreateClient(string name)
        {
            return httpClient;
        }
    }

    private sealed class StubHttpMessageHandler(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return handler(request, cancellationToken);
        }
    }

    private sealed class RecordingLogger<T> : ILogger<T>
    {
        public List<string> Messages { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
        {
            return NullScope.Instance;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Messages.Add(formatter(state, exception));
        }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();

            public void Dispose()
            {
            }
        }
    }
}
