using FluentValidation;
using LinkedInContentManager.Application.Common;
using LinkedInContentManager.Application.ExternalServices.Auth;
using LinkedInContentManager.Contracts.Auth;
using LinkedInContentManager.Domain.Entities;
using LinkedInContentManager.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LinkedInContentManager.Application.Features.Auth;

/// <summary>Self-registration defaults to <see cref="UserRole.ContentManager"/> — enough access to run the product (campaigns, approvals). <see cref="UserRole.Admin"/> is granted manually, not through self-service registration.</summary>
public sealed record RegisterCommand(RegisterRequest Request) : IRequest<AuthResponse>;

public sealed class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.Request.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Request.Password).NotEmpty().MinimumLength(8).MaximumLength(200);
        RuleFor(x => x.Request.DisplayName).NotEmpty().MaximumLength(200);
    }
}

public sealed class RegisterCommandHandler(IApplicationDbContext db, IPasswordHasher passwordHasher, ITokenService tokenService)
    : IRequestHandler<RegisterCommand, AuthResponse>
{
    public async Task<AuthResponse> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var req = request.Request;
        var normalizedEmail = req.Email.Trim().ToLowerInvariant();

        if (await db.Users.AnyAsync(u => u.Email == normalizedEmail, cancellationToken))
            throw new InvalidOperationException($"An account with email '{normalizedEmail}' already exists.");

        var hash = passwordHasher.HashPassword(req.Password);
        var user = new User(normalizedEmail, hash, req.DisplayName, UserRole.ContentManager);

        db.Users.Add(user);

        var access = tokenService.GenerateAccessToken(user);
        var refresh = tokenService.GenerateRefreshToken();
        user.SetRefreshToken(refresh.Hash, refresh.ExpiresAtUtc);

        await db.SaveChangesAsync(cancellationToken);

        return new AuthResponse(access.Value, access.ExpiresAtUtc, refresh.Value, refresh.ExpiresAtUtc, user.Id, user.Email, user.DisplayName, user.Role);
    }
}
