using System.ComponentModel.DataAnnotations;

namespace LinkedInContentManager.Infrastructure.ExternalServices.CrewAi;

/// <summary>Bound from the <c>CrewAiService</c> configuration section.</summary>
public sealed class CrewAiServiceOptions
{
    public const string SectionName = "CrewAiService";

    [Required]
    public required string BaseUrl { get; set; }

    /// <summary>Sent as <c>X-Service-Api-Key</c>. Null/empty disables the header (matches the service's own open-for-dev default).</summary>
    public string? ApiKey { get; set; }

    public int TimeoutSeconds { get; set; } = 180;

    public int RetryCount { get; set; } = 2;

    public int CircuitBreakerFailureThreshold { get; set; } = 5;
}
