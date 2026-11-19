using System.Reflection;
using NetArchTest.Rules;

namespace LinkedInContentManager.ArchitectureTests;

/// <summary>
/// Enforces the dependency rules documented in docs/architecture.md as executable tests —
/// if someone adds a `using LinkedInContentManager.Infrastructure` to the Domain project,
/// this fails the build instead of silently rotting the architecture.
/// </summary>
public class CleanArchitectureTests
{
    private static readonly Assembly DomainAssembly = typeof(Domain.Entities.Campaign).Assembly;
    private static readonly Assembly ApplicationAssembly = typeof(Application.DependencyInjection).Assembly;
    private static readonly Assembly InfrastructureAssembly = typeof(Infrastructure.DependencyInjection).Assembly;
    private static readonly Assembly ContractsAssembly = typeof(Contracts.Common.PagedResult<object>).Assembly;

    [Fact]
    public void Domain_Should_Not_DependOn_AnyOtherLayer()
    {
        var result = Types.InAssembly(DomainAssembly)
            .Should()
            .NotHaveDependencyOnAny(
                "LinkedInContentManager.Application",
                "LinkedInContentManager.Infrastructure",
                "LinkedInContentManager.Contracts",
                "LinkedInContentManager.API",
                "LinkedInContentManager.Worker")
            .GetResult();

        Assert.True(result.IsSuccessful, Describe(result));
    }

    [Fact]
    public void Contracts_Should_Only_DependOn_Domain()
    {
        var result = Types.InAssembly(ContractsAssembly)
            .Should()
            .NotHaveDependencyOnAny(
                "LinkedInContentManager.Application",
                "LinkedInContentManager.Infrastructure",
                "LinkedInContentManager.API",
                "LinkedInContentManager.Worker")
            .GetResult();

        Assert.True(result.IsSuccessful, Describe(result));
    }

    [Fact]
    public void Application_Should_Not_DependOn_InfrastructureOrApi()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .Should()
            .NotHaveDependencyOnAny(
                "LinkedInContentManager.Infrastructure",
                "LinkedInContentManager.API",
                "LinkedInContentManager.Worker")
            .GetResult();

        Assert.True(result.IsSuccessful, Describe(result));
    }

    [Fact]
    public void Infrastructure_Should_Not_DependOn_Api()
    {
        var result = Types.InAssembly(InfrastructureAssembly)
            .Should()
            .NotHaveDependencyOnAny("LinkedInContentManager.API", "LinkedInContentManager.Worker")
            .GetResult();

        Assert.True(result.IsSuccessful, Describe(result));
    }

    [Fact]
    public void DomainEntities_Should_Not_HavePublicSetters()
    {
        // Every entity mutation must go through a named business-rule method, never a bare
        // setter — this is what makes the live-tested state-machine guards (AgentExecution,
        // PublishingSchedule, ContentApproval) actually enforceable.
        var entityTypes = Types.InAssembly(DomainAssembly)
            .That().ResideInNamespace("LinkedInContentManager.Domain.Entities")
            .GetTypes();

        var violations = entityTypes
            .SelectMany(t => t.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            .Where(p => p.SetMethod is { IsPublic: true })
            .Select(p => $"{p.DeclaringType!.Name}.{p.Name}")
            .ToList();

        Assert.True(violations.Count == 0, $"Entities with public setters: {string.Join(", ", violations)}");
    }

    [Fact]
    public void Application_Should_HaveAHandlerForEveryCommandAndQuery()
    {
        var handlerCount = ApplicationAssembly.GetTypes()
            .Count(t => t.GetInterfaces().Any(i => i.IsGenericType &&
                (i.GetGenericTypeDefinition() == typeof(MediatR.IRequestHandler<,>)
                 || i.GetGenericTypeDefinition() == typeof(MediatR.IRequestHandler<>))));

        Assert.True(handlerCount >= 15, $"Expected at least 15 MediatR handlers (one per command/query), found {handlerCount}.");
    }

    private static string Describe(TestResult result) =>
        result.FailingTypes is null ? "no detail" : string.Join(", ", result.FailingTypes.Select(t => t.FullName));
}
