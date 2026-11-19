namespace LinkedInContentManager.Application.ExternalServices.CrewAi;

/// <summary>
/// Raised by <see cref="ICrewAiClient"/> on any non-success response or transport failure.
/// The exception message is already sanitized by the time it reaches here — never put a raw
/// response body or secret into it.
/// </summary>
public sealed class CrewAiServiceException : Exception
{
    public int? StatusCode { get; }

    public CrewAiServiceException(string message, int? statusCode = null, Exception? innerException = null)
        : base(message, innerException)
    {
        StatusCode = statusCode;
    }
}
