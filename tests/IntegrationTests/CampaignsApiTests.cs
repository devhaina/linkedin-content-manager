using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using LinkedInContentManager.Contracts.Campaigns;
using LinkedInContentManager.Contracts.Common;
using LinkedInContentManager.Domain.Enums;
using LinkedInContentManager.Infrastructure.Persistence.Seed;

namespace LinkedInContentManager.IntegrationTests;

[Collection("Api")]
public class CampaignsApiTests(CustomWebApplicationFactory factory)
{
    [Fact]
    public async Task Create_WithoutAuth_Returns401()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/campaigns",
            new CreateCampaignRequest("X", NicheSeedData.FintechId, WriterPersona.Practitioner), TestJsonOptions.Options);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Create_WithAuth_Returns201_OwnedByCaller()
    {
        var (client, auth) = await factory.CreateAuthenticatedClientAsync();

        var response = await client.PostAsJsonAsync("/api/campaigns",
            new CreateCampaignRequest("Fintech Push", NicheSeedData.FintechId, WriterPersona.Practitioner), TestJsonOptions.Options);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<CampaignResponse>(TestJsonOptions.Options);
        body!.OwnerUserId.Should().Be(auth.UserId);
        body.NicheName.Should().Be("Fintech");
        body.Status.Should().Be(CampaignStatus.Active);
    }

    [Fact]
    public async Task Create_WithUnknownNiche_Returns404()
    {
        var (client, _) = await factory.CreateAuthenticatedClientAsync();

        var response = await client.PostAsJsonAsync("/api/campaigns",
            new CreateCampaignRequest("X", Guid.NewGuid(), WriterPersona.Practitioner), TestJsonOptions.Options);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetById_ThenUpdate_ThenDelete_Archives_NotHardDeletes()
    {
        var (client, _) = await factory.CreateAuthenticatedClientAsync();
        var created = await (await client.PostAsJsonAsync("/api/campaigns",
            new CreateCampaignRequest("Lifecycle Test", NicheSeedData.CryptoId, WriterPersona.ConversationalApproachable), TestJsonOptions.Options))
            .Content.ReadFromJsonAsync<CampaignResponse>(TestJsonOptions.Options);

        var getResponse = await client.GetAsync($"/api/campaigns/{created!.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var updateResponse = await client.PutAsJsonAsync($"/api/campaigns/{created.Id}",
            new UpdateCampaignRequest("Renamed", WriterPersona.FormalAuthoritative, CampaignStatus.Paused), TestJsonOptions.Options);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await updateResponse.Content.ReadFromJsonAsync<CampaignResponse>(TestJsonOptions.Options);
        updated!.Name.Should().Be("Renamed");
        updated.Status.Should().Be(CampaignStatus.Paused);

        var deleteResponse = await client.DeleteAsync($"/api/campaigns/{created.Id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var afterDelete = await (await client.GetAsync($"/api/campaigns/{created.Id}")).Content.ReadFromJsonAsync<CampaignResponse>(TestJsonOptions.Options);
        afterDelete!.Status.Should().Be(CampaignStatus.Archived); // still exists — archived, not hard-deleted
    }

    [Fact]
    public async Task GetAll_FiltersByNiche()
    {
        var (client, _) = await factory.CreateAuthenticatedClientAsync();
        await client.PostAsJsonAsync("/api/campaigns",
            new CreateCampaignRequest("Niche Filter Test", NicheSeedData.FintechSecurityId, WriterPersona.Practitioner), TestJsonOptions.Options);

        var response = await client.GetAsync($"/api/campaigns?nicheId={NicheSeedData.FintechSecurityId}&pageSize=50");

        var body = await response.Content.ReadFromJsonAsync<PagedResult<CampaignResponse>>(TestJsonOptions.Options);
        body!.Items.Should().OnlyContain(c => c.NicheId == NicheSeedData.FintechSecurityId);
    }
}
