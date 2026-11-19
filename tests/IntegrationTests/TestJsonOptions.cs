using System.Text.Json;
using System.Text.Json.Serialization;

namespace LinkedInContentManager.IntegrationTests;

/// <summary>
/// Must mirror the server's own JSON config (`AddJsonOptions` in <c>Program.cs</c>, which adds
/// <see cref="JsonStringEnumConverter"/>) — without this, every response containing an enum
/// (role, status, persona, ...) fails to deserialize client-side with the default options,
/// even though the server serialized it correctly.
/// </summary>
internal static class TestJsonOptions
{
    public static readonly JsonSerializerOptions Options = new()
    {
        // The server (ASP.NET Core's default MVC JSON config) serializes camelCase and
        // System.Text.Json is case-SENSITIVE by default — without this, every property
        // silently binds to null/default instead of throwing, which is what made the real bug
        // here so confusing: a 201 Created with a totally empty-looking body.
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };
}
