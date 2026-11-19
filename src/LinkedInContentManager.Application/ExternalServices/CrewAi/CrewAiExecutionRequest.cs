using LinkedInContentManager.Domain.Enums;

namespace LinkedInContentManager.Application.ExternalServices.CrewAi;

/// <summary>One call to the CrewAI service's <c>POST /api/v1/execute</c>.</summary>
public sealed record CrewAiExecutionRequest(string Topic, string Niche, WriterPersona Persona, string CorrelationId);
