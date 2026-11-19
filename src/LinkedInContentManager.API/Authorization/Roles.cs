namespace LinkedInContentManager.API.Authorization;

/// <summary>Role name strings for <see cref="Microsoft.AspNetCore.Authorization.AuthorizeAttribute.Roles"/>, matching <see cref="LinkedInContentManager.Domain.Enums.UserRole"/>'s member names exactly.</summary>
public static class Roles
{
    public const string Admin = "Admin";
    public const string ContentManager = "ContentManager";
    public const string Viewer = "Viewer";

    /// <summary>Anyone who can mutate campaigns/content/schedules — everything except read-only Viewer.</summary>
    public const string Editors = "Admin,ContentManager";
}
