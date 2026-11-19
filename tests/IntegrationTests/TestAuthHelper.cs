using System.Net.Http.Headers;
using System.Net.Http.Json;
using LinkedInContentManager.Contracts.Auth;

namespace LinkedInContentManager.IntegrationTests;

internal static class TestAuthHelper
{
    /// <summary>Registers a brand-new unique user and returns an <see cref="HttpClient"/> with its real JWT attached.</summary>
    public static async Task<(HttpClient Client, AuthResponse Auth)> CreateAuthenticatedClientAsync(this CustomWebApplicationFactory factory)
    {
        var client = factory.CreateClient();
        var request = new RegisterRequest($"user-{Guid.NewGuid():N}@example.com", "SuperSecret123!", "Test User");

        var response = await client.PostAsJsonAsync("/api/auth/register", request, TestJsonOptions.Options);
        response.EnsureSuccessStatusCode();
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>(TestJsonOptions.Options);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
        return (client, auth);
    }
}
