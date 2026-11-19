namespace LinkedInContentManager.Application.ExternalServices.Messaging;

/// <summary>Publishes a content-generation job onto the RabbitMQ queue the Worker consumes. Implemented in Infrastructure.</summary>
public interface IContentGenerationJobPublisher
{
    Task PublishAsync(ContentGenerationJobMessage message, CancellationToken cancellationToken);
}
