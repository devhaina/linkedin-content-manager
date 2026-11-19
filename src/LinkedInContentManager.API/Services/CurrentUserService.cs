using System.Security.Claims;
using LinkedInContentManager.Application.Common;
using LinkedInContentManager.Domain.Enums;

namespace LinkedInContentManager.API.Services;

public sealed class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    public Guid UserId
    {
        get
        {
            var value = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(value, out var id) ? id : throw new UnauthorizedAccessException("No authenticated user on the current request.");
        }
    }

    public UserRole Role
    {
        get
        {
            var value = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Role);
            return Enum.TryParse<UserRole>(value, out var role) ? role : throw new UnauthorizedAccessException("No authenticated user on the current request.");
        }
    }
}
