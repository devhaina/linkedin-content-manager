using System.Text;
using System.Text.Json;
using LinkedInContentManager.Application.Common;
using LinkedInContentManager.Application.ExternalServices.CrewAi;
using LinkedInContentManager.Application.ExternalServices.Messaging;
using LinkedInContentManager.Domain.Entities;
using LinkedInContentManager.Domain.Enums;
using LinkedInContentManager.Infrastructure.ExternalServices.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace LinkedInContentManager.Worker.Consumers;

/// <summary>
/// Dequeues <see cref="ContentGenerationJobMessage"/>s and drives the real CrewAI pipeline run
/// to completion: calls <see cref="ICrewAiClient"/>, persists every stage's output as real
/// entities (never fabricated), and acks/nacks based on genuine outcome. One DI scope per
/// message — <see cref="IApplicationDbContext"/> is not safe to share across messages.
/// </summary>
public sealed class ContentGenerationJobConsumer(
    RabbitMqConnectionManager connectionManager,
    IOptions<RabbitMqOptions> rabbitOptions,
    IServiceScopeFactory scopeFactory,
    ILogger<ContentGenerationJobConsumer> logger) : BackgroundService
{
    private const int MaxRetries = 2;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var queueName = rabbitOptions.Value.ContentGenerationQueueName;

        var connection = await connectionManager.GetConnectionAsync(stoppingToken);
        var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

        await channel.QueueDeclareAsync(queueName, durable: true, exclusive: false, autoDelete: false, cancellationToken: stoppingToken);
        await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false, cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, ea) => await HandleMessageAsync(channel, ea, stoppingToken);

        await channel.BasicConsumeAsync(queueName, autoAck: false, consumer, cancellationToken: stoppingToken);

        logger.LogInformation("Content generation consumer listening on queue '{QueueName}'", queueName);

        // BasicConsumeAsync registers the callback and returns immediately — keep this
        // ExecuteAsync task alive for the service's lifetime instead of letting it complete.
        await Task.Delay(Timeout.Infinite, stoppingToken).ContinueWith(_ => { }, TaskScheduler.Default);
    }

    private async Task HandleMessageAsync(IChannel channel, BasicDeliverEventArgs ea, CancellationToken stoppingToken)
    {
        ContentGenerationJobMessage? message;
        try
        {
            message = JsonSerializer.Deserialize<ContentGenerationJobMessage>(Encoding.UTF8.GetString(ea.Body.ToArray()), JsonOptions);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to deserialize content-generation job message — discarding poison message.");
            await channel.BasicAckAsync(ea.DeliveryTag, multiple: false, stoppingToken);
            return;
        }

        if (message is null)
        {
            await channel.BasicAckAsync(ea.DeliveryTag, multiple: false, stoppingToken);
            return;
        }

        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var crewAiClient = scope.ServiceProvider.GetRequiredService<ICrewAiClient>();

        using var _ = logger.BeginScope(new Dictionary<string, object>
        {
            ["AgentExecutionId"] = message.AgentExecutionId,
            ["CorrelationId"] = message.CorrelationId
        });

        try
        {
            await ProcessJobAsync(db, crewAiClient, message, stoppingToken);
            await channel.BasicAckAsync(ea.DeliveryTag, multiple: false, stoppingToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Content generation job failed for AgentExecutionId={AgentExecutionId}", message.AgentExecutionId);
            var shouldRequeue = await RecordFailureAsync(db, message.AgentExecutionId, ex, stoppingToken);
            await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: shouldRequeue, stoppingToken);
        }
    }

    private static async Task ProcessJobAsync(IApplicationDbContext db, ICrewAiClient crewAiClient, ContentGenerationJobMessage message, CancellationToken cancellationToken)
    {
        var execution = await db.AgentExecutions.FirstOrDefaultAsync(e => e.Id == message.AgentExecutionId, cancellationToken)
            ?? throw new InvalidOperationException($"AgentExecution '{message.AgentExecutionId}' was not found — it must be created before the job is enqueued.");

        // Mark Running and save BEFORE the slow external call, so GET /api/content/{id}
        // reflects "actually in progress" for the minutes the pipeline run actually takes,
        // instead of sitting on "Pending" the whole time.
        execution.Start();
        await db.SaveChangesAsync(cancellationToken);

        var result = await crewAiClient.ExecuteContentPipelineAsync(
            new CrewAiExecutionRequest(message.Topic, message.Niche, message.Persona, message.CorrelationId),
            cancellationToken);

        execution.SetExternalExecutionId(result.ExecutionId);

        if (result.Status != "Completed" || result.Research is null || result.Writing is null ||
            result.Critique is null || result.Optimization is null || result.Scheduling is null)
        {
            execution.Fail(AgentName.SchedulingAgent, result.Error ?? "CrewAI pipeline did not return a complete result for all five stages.");
            await db.SaveChangesAsync(cancellationToken);
            return;
        }

        var research = new ResearchResult(
            execution.Id, result.Research.TrendingContentAngles, result.Research.TrendingHashtags,
            result.Research.TrendingHooks, result.Research.SourceUrls, result.Research.Summary);
        execution.AttachResearchResult(research);
        db.ResearchResults.Add(research);

        var post = new LinkedInPost(execution.Id, message.Persona, result.Writing.DraftContent);
        execution.AttachLinkedInPost(post);
        db.LinkedInPosts.Add(post);

        post.AddReview(
            result.Critique.Score, result.Critique.HookEvaluation, result.Critique.StorytellingEvaluation,
            result.Critique.EngagementPotential, result.Critique.CtaEffectiveness, result.Critique.ToneReadabilityAssessment,
            result.Critique.GuardrailViolations, result.Critique.Recommendations);

        post.ApplyOptimization(result.Optimization.OptimizedContent);

        var hashtagTexts = result.Optimization.OptimizedHashtags.Count > 0
            ? result.Optimization.OptimizedHashtags
            : result.Writing.SuggestedHashtags;

        var position = 0;
        foreach (var rawText in hashtagTexts)
        {
            var normalized = Hashtag.Normalize(rawText);
            var hashtag = await db.Hashtags.FirstOrDefaultAsync(h => h.Text == normalized, cancellationToken);
            if (hashtag is null)
            {
                hashtag = new Hashtag(normalized);
                db.Hashtags.Add(hashtag);
            }
            hashtag.IncrementUsage();
            post.AttachHashtag(hashtag.Id, position++);
        }

        var approval = new ContentApproval(post.Id);
        post.SetApproval(approval);
        db.ContentApprovals.Add(approval);

        var schedule = new PublishingSchedule(
            post.Id, result.Scheduling.RecommendedDay, result.Scheduling.RecommendedTime, result.Scheduling.AudienceTimezone,
            result.Scheduling.HashtagStrategy, result.Scheduling.FirstHourEngagementPlan);
        post.SetPublishingSchedule(schedule);
        db.PublishingSchedules.Add(schedule);

        execution.Complete();

        await db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Records the failure and returns whether the message should be requeued for another attempt.</summary>
    private async Task<bool> RecordFailureAsync(IApplicationDbContext db, Guid agentExecutionId, Exception ex, CancellationToken cancellationToken)
    {
        var execution = await db.AgentExecutions.FirstOrDefaultAsync(e => e.Id == agentExecutionId, cancellationToken);
        if (execution is null)
            return false;

        if (execution.RetryCount < MaxRetries)
        {
            execution.RegisterRetry();
            await db.SaveChangesAsync(cancellationToken);
            logger.LogWarning("Retry {RetryCount}/{MaxRetries} scheduled for AgentExecutionId={AgentExecutionId}", execution.RetryCount, MaxRetries, agentExecutionId);
            return true;
        }

        execution.Fail(AgentName.SchedulingAgent, Sanitize(ex.Message));
        await db.SaveChangesAsync(cancellationToken);
        return false;
    }

    private static string Sanitize(string message) =>
        System.Text.RegularExpressions.Regex.Replace(message, @"sk-[A-Za-z0-9_\-]{10,}", "[redacted]");
}
