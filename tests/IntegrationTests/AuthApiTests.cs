using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using LinkedInContentManager.Contracts.Auth;

namespace LinkedInContentManager.IntegrationTests;

[Collection("Api")]
public class AuthApiTests(CustomWebApplicationFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Register_WithValidData_Returns201AndTokens()
    {
        var request = new RegisterRequest($"user-{Guid.NewGuid():N}@example.com", "SuperSecret123!", "Test User");

        var response = await _client.PostAsJsonAsync("/api/auth/register", request, TestJsonOptions.Options);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>(TestJsonOptions.Options);
        body!.AccessToken.Should().NotBeNullOrWhiteSpace();
        body.Role.Should().Be(LinkedInContentManager.Domain.Enums.UserRole.ContentManager);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_Returns409()
    {
        var email = $"dup-{Guid.NewGuid():N}@example.com";
        var request = new RegisterRequest(email, "SuperSecret123!", "First");
        await _client.PostAsJsonAsync("/api/auth/register", request, TestJsonOptions.Options);

        var response = await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, "AnotherPass123!", "Second"), TestJsonOptions.Options);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns401()
    {
        var email = $"wrongpw-{Guid.NewGuid():N}@example.com";
        await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, "CorrectPass123!", "User"), TestJsonOptions.Options);

        var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, "WrongPass123!"), TestJsonOptions.Options);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WithCorrectPassword_ReturnsTokens()
    {
        var email = $"login-{Guid.NewGuid():N}@example.com";
        await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, "CorrectPass123!", "User"), TestJsonOptions.Options);

        var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, "CorrectPass123!"), TestJsonOptions.Options);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>(TestJsonOptions.Options);
        body!.AccessToken.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task RefreshToken_ReusedAfterRotation_Returns401()
    {
        var email = $"refresh-{Guid.NewGuid():N}@example.com";
        await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, "CorrectPass123!", "User"), TestJsonOptions.Options);
        var login = await (await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, "CorrectPass123!"), TestJsonOptions.Options))
            .Content.ReadFromJsonAsync<AuthResponse>(TestJsonOptions.Options);

        var first = await _client.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenRequest(login!.RefreshToken), TestJsonOptions.Options);
        first.StatusCode.Should().Be(HttpStatusCode.OK);

        var second = await _client.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenRequest(login.RefreshToken), TestJsonOptions.Options);
        second.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
