using System.ComponentModel.DataAnnotations;

namespace LinkedInContentManager.Infrastructure.ExternalServices.Messaging;

/// <summary>Bound from the <c>RabbitMq</c> configuration section.</summary>
public sealed class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    [Required]
    public required string HostName { get; set; }

    public int Port { get; set; } = 5672;

    [Required]
    public required string UserName { get; set; }

    [Required]
    public required string Password { get; set; }

    public string VirtualHost { get; set; } = "/";

    public string ContentGenerationQueueName { get; set; } = "content-generation-jobs";
}
