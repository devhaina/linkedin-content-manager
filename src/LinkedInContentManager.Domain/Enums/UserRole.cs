namespace LinkedInContentManager.Domain.Enums;

/// <summary>Role-based authorization tiers for the API's JWT roles claim.</summary>
public enum UserRole
{
    Viewer = 0,
    ContentManager = 1,
    Admin = 2
}
