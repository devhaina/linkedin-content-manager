using FluentValidation;
using LinkedInContentManager.Application.Common;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace LinkedInContentManager.Application;

/// <summary>Registers everything the Application layer owns: MediatR pipeline and FluentValidation validators.</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        return services;
    }
}
