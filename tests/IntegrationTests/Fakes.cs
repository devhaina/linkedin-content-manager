using LinkedInContentManager.Application.ExternalServices.CrewAi;
using LinkedInContentManager.Application.ExternalServices.Messaging;

namespace LinkedInContentManager.IntegrationTests;

/// <summary>
/// Records calls instead of making a real (slow, billed) CrewAI call. Used only by the fast
/// API integration tests — <see cref="CrewAiHttpClientTests"/> exercises the real client
/// against the real service separately.
/// </summary>
public sealed class FakeCrewAiClient : ICrewAiClient
{
    public List<CrewAiExecutionRequest> Requests { get; } = [];

    public Task<CrewAiExecutionResult> ExecuteContentPipelineAsync(CrewAiExecutionRequest request, CancellationToken cancellationToken)
    {
        Requests.Add(request);
        throw new CrewAiServiceException("FakeCrewAiClient does not actually run the pipeline — this path is not exercised by these fast tests.");
    }
}

/// <summary>Records enqueued jobs instead of publishing to a real RabbitMQ broker.</summary>
public sealed class FakeContentGenerationJobPublisher : IContentGenerationJobPublisher
{
    public List<ContentGenerationJobMessage> PublishedMessages { get; } = [];

    public Task PublishAsync(ContentGenerationJobMessage message, CancellationToken cancellationToken)
    {
        PublishedMessages.Add(message);
        return Task.CompletedTask;
    }
}
