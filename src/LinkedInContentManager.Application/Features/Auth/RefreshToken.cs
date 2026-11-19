using LinkedInContentManager.Application.Common;
using LinkedInContentManager.Application.ExternalServices.Auth;
using LinkedInContentManager.Contracts.Auth;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LinkedInContentManager.Application.Features.Auth;

public sealed record RefreshTokenCommand(RefreshTokenRequest Request) : IRequest<AuthResponse>;

public sealed class RefreshTokenCommandHandler(IApplicationDbContext db, ITokenService tokenService)
    : IRequestHandler<RefreshTokenCommand, AuthResponse>
{
    public async Task<AuthResponse> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var presentedHash = tokenService.HashRefreshToken(request.Request.RefreshToken);

        var user = await db.Users.FirstOrDefaultAsync(u => u.RefreshTokenHash == presentedHash, cancellationToken);

        if (user is null || !user.IsActive || user.RefreshTokenExpiresAtUtc is null || user.RefreshTokenExpiresAtUtc < DateTimeOffset.UtcNow)
            throw new UnauthorizedAccessException("Refresh token is invalid or expired.");

        var access = tokenService.GenerateAccessToken(user);
        var refresh = tokenService.GenerateRefreshToken();
        user.SetRefreshToken(refresh.Hash, refresh.ExpiresAtUtc); // rotated — the presented token is single-use

        await db.SaveChangesAsync(cancellationToken);

        return new AuthResponse(access.Value, access.ExpiresAtUtc, refresh.Value, refresh.ExpiresAtUtc, user.Id, user.Email, user.DisplayName, user.Role);
    }
}
