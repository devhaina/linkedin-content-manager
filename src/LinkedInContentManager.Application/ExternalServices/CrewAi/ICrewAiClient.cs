namespace LinkedInContentManager.Application.ExternalServices.CrewAi;

/// <summary>
/// Abstraction over the CrewAI Python microservice's five-agent pipeline. Implemented in
/// Infrastructure as a strongly-typed <see cref="System.Net.Http.HttpClient"/> wrapper so
/// Application handlers never see HTTP, Polly, or JSON concerns directly.
/// </summary>
public interface ICrewAiClient
{
    /// <summary>
    /// Runs the full Research → Writing → Critique → Optimization → Scheduling pipeline for
    /// one topic and returns every stage's output. Throws <see cref="CrewAiServiceException"/>
    /// on any failure — callers must not translate a thrown exception into a fabricated result.
    /// </summary>
    Task<CrewAiExecutionResult> ExecuteContentPipelineAsync(CrewAiExecutionRequest request, CancellationToken cancellationToken);
}
