using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using LinkedInContentManager.Contracts.Campaigns;
using LinkedInContentManager.Domain.Enums;
using LinkedInContentManager.Infrastructure.Persistence;
using LinkedInContentManager.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LinkedInContentManager.IntegrationTests;

/// <summary>Mirrors the live role-downgrade check done manually in Phase 6 — here as a repeatable automated test.</summary>
[Collection("Api")]
public class RoleAuthorizationApiTests(CustomWebApplicationFactory factory)
{
    [Fact]
    public async Task Viewer_CanRead_ButCannotCreateCampaign()
    {
        var (client, auth) = await factory.CreateAuthenticatedClientAsync();

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var user = await db.Users.FirstAsync(u => u.Id == auth.UserId);
            db.Entry(user).Property("Role").CurrentValue = UserRole.Viewer;
            await db.SaveChangesAsync();
        }

        // Re-login to get a fresh token carrying the Viewer role claim.
        var relogin = await (await client.PostAsJsonAsync("/api/auth/login",
            new LinkedInContentManager.Contracts.Auth.LoginRequest(auth.Email, "SuperSecret123!"), TestJsonOptions.Options))
            .Content.ReadFromJsonAsync<LinkedInContentManager.Contracts.Auth.AuthResponse>(TestJsonOptions.Options);
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", relogin!.AccessToken);

        var readResponse = await client.GetAsync("/api/campaigns");
        readResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var writeResponse = await client.PostAsJsonAsync("/api/campaigns",
            new CreateCampaignRequest("Should be forbidden", NicheSeedData.FintechId, WriterPersona.Practitioner), TestJsonOptions.Options);
        writeResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
