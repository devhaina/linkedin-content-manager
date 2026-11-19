using LinkedInContentManager.Domain.Enums;

namespace LinkedInContentManager.Application.Common;

/// <summary>
/// The authenticated caller, read from the validated JWT. Implemented in the API layer (reads
/// <c>HttpContext.User</c>) so Application handlers never see <c>ClaimsPrincipal</c> or ASP.NET
/// Core types directly. Replaces the explicit <c>ReviewerUserId</c>/<c>OwnerUserId</c> request
/// fields Phase 5 used before real authentication existed.
/// </summary>
public interface ICurrentUserService
{
    Guid UserId { get; }
    UserRole Role { get; }
}
