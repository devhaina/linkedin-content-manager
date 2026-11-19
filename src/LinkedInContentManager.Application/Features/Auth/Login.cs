using FluentValidation;
using LinkedInContentManager.Application.Common;
using LinkedInContentManager.Application.ExternalServices.Auth;
using LinkedInContentManager.Contracts.Auth;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LinkedInContentManager.Application.Features.Auth;

public sealed record LoginCommand(LoginRequest Request) : IRequest<AuthResponse>;

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Request.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Request.Password).NotEmpty();
    }
}

public sealed class LoginCommandHandler(IApplicationDbContext db, IPasswordHasher passwordHasher, ITokenService tokenService)
    : IRequestHandler<LoginCommand, AuthResponse>
{
    public async Task<AuthResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Request.Email.Trim().ToLowerInvariant();

        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);

        // Deliberately the same error for "no such user" and "wrong password" — distinguishing
        // them lets an attacker enumerate valid emails.
        if (user is null || !user.IsActive || !passwordHasher.VerifyPassword(user, user.PasswordHash, request.Request.Password))
            throw new UnauthorizedAccessException("Invalid email or password.");

        var access = tokenService.GenerateAccessToken(user);
        var refresh = tokenService.GenerateRefreshToken();
        user.SetRefreshToken(refresh.Hash, refresh.ExpiresAtUtc);

        await db.SaveChangesAsync(cancellationToken);

        return new AuthResponse(access.Value, access.ExpiresAtUtc, refresh.Value, refresh.ExpiresAtUtc, user.Id, user.Email, user.DisplayName, user.Role);
    }
}
