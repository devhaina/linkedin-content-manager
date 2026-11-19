using LinkedInContentManager.Domain.Enums;

namespace LinkedInContentManager.Application.ExternalServices.Messaging;

/// <summary>The message enqueued onto RabbitMQ for the Worker to pick up and run through the CrewAI pipeline.</summary>
public sealed record ContentGenerationJobMessage(
    Guid AgentExecutionId,
    Guid ContentTopicId,
    string Topic,
    string Niche,
    WriterPersona Persona,
    string CorrelationId);
