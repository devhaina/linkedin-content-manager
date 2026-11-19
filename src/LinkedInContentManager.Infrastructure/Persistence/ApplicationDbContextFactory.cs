using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.DependencyInjection;

namespace LinkedInContentManager.Infrastructure.Persistence;

/// <summary>
/// Lets `dotnet ef migrations` run without the full API host (and its JWT/Serilog/RabbitMQ
/// setup) spinning up. Only used by EF Core design-time tooling — never by the running app,
/// which registers <see cref="ApplicationDbContext"/> through <c>Program.cs</c> DI instead.
/// </summary>
public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING__PostgreSql")
            ?? "Host=localhost;Port=5432;Database=linkedin_content_manager;Username=postgres;Password=postgres";

        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(connectionString);

        var services = new ServiceCollection()
            .AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(ApplicationDbContextFactory).Assembly));
        var publisher = services.BuildServiceProvider().GetRequiredService<IPublisher>();

        return new ApplicationDbContext(optionsBuilder.Options, publisher);
    }
}
