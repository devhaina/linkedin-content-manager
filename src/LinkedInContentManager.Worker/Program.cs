using LinkedInContentManager.Application;
using LinkedInContentManager.Infrastructure;
using LinkedInContentManager.Worker.Consumers;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddHostedService<ContentGenerationJobConsumer>();

var host = builder.Build();
host.Run();
