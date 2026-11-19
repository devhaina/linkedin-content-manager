using System.Text;
using System.Text.Json;
using LinkedInContentManager.Application.ExternalServices.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace LinkedInContentManager.Infrastructure.ExternalServices.Messaging;

public sealed class RabbitMqContentGenerationJobPublisher(
    RabbitMqConnectionManager connectionManager,
    IOptions<RabbitMqOptions> options,
    ILogger<RabbitMqContentGenerationJobPublisher> logger) : IContentGenerationJobPublisher
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public async Task PublishAsync(ContentGenerationJobMessage message, CancellationToken cancellationToken)
    {
        var queueName = options.Value.ContentGenerationQueueName;

        var connection = await connectionManager.GetConnectionAsync(cancellationToken);
        using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);

        await channel.QueueDeclareAsync(
            queue: queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: cancellationToken);

        var body = JsonSerializer.SerializeToUtf8Bytes(message, JsonOptions);

        var properties = new BasicProperties
        {
            Persistent = true,
            ContentType = "application/json",
            CorrelationId = message.CorrelationId,
            MessageId = message.AgentExecutionId.ToString(),
        };

        await channel.BasicPublishAsync(
            exchange: string.Empty,
            routingKey: queueName,
            mandatory: false,
            basicProperties: properties,
            body: body,
            cancellationToken: cancellationToken);

        logger.LogInformation(
            "Published content-generation job for AgentExecutionId={AgentExecutionId} correlationId={CorrelationId}",
            message.AgentExecutionId, message.CorrelationId);
    }
}
