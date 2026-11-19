using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using LinkedInContentManager.Contracts.Campaigns;
using LinkedInContentManager.Contracts.Content;
using LinkedInContentManager.Domain.Enums;
using LinkedInContentManager.Infrastructure.Persistence.Seed;

namespace LinkedInContentManager.IntegrationTests;

[Collection("Api")]
public class ContentApiTests(CustomWebApplicationFactory factory)
{
    private async Task<(HttpClient Client, CampaignResponse Campaign)> CreateClientWithCampaignAsync()
    {
        var (client, _) = await factory.CreateAuthenticatedClientAsync();
        var campaign = await (await client.PostAsJsonAsync("/api/campaigns",
            new CreateCampaignRequest("Content Test Campaign", NicheSeedData.EmergingAiId, WriterPersona.Practitioner), TestJsonOptions.Options))
            .Content.ReadFromJsonAsync<CampaignResponse>(TestJsonOptions.Options);
        return (client, campaign!);
    }

    [Fact]
    public async Task Generate_EnqueuesJob_AndReturnsPendingExecution()
    {
        var (client, campaign) = await CreateClientWithCampaignAsync();
        factory.FakePublisher.PublishedMessages.Clear();

        var response = await client.PostAsJsonAsync("/api/content/generate",
            new GenerateContentRequest(campaign.Id, "AI agents automating customer support"), TestJsonOptions.Options);

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var body = await response.Content.ReadFromJsonAsync<GenerateContentResponse>(TestJsonOptions.Options);
        body!.Status.Should().Be(ExecutionStatus.Pending);

        factory.FakePublisher.PublishedMessages.Should().ContainSingle(m => m.AgentExecutionId == body.ExecutionId);
    }

    [Fact]
    public async Task GetById_ForFreshExecution_ShowsPendingWithNoContentYet()
    {
        var (client, campaign) = await CreateClientWithCampaignAsync();
        var generated = await (await client.PostAsJsonAsync("/api/content/generate",
            new GenerateContentRequest(campaign.Id, "Zero-trust architecture for fintech APIs"), TestJsonOptions.Options))
            .Content.ReadFromJsonAsync<GenerateContentResponse>(TestJsonOptions.Options);

        var response = await client.GetAsync($"/api/content/{generated!.ExecutionId}");

        var detail = await response.Content.ReadFromJsonAsync<ContentDetailResponse>(TestJsonOptions.Options);
        detail!.Status.Should().Be(ExecutionStatus.Pending);
        detail.DraftContent.Should().BeNull();
        detail.Research.Should().BeNull();
    }

    [Fact]
    public async Task Approve_BeforeAnyContentExists_Returns409()
    {
        var (client, campaign) = await CreateClientWithCampaignAsync();
        var generated = await (await client.PostAsJsonAsync("/api/content/generate",
            new GenerateContentRequest(campaign.Id, "Quantum-resistant cryptography basics"), TestJsonOptions.Options))
            .Content.ReadFromJsonAsync<GenerateContentResponse>(TestJsonOptions.Options);

        var response = await client.PostAsJsonAsync($"/api/content/{generated!.ExecutionId}/approve", new ApproveContentRequest(), TestJsonOptions.Options);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task GetById_ForUnknownExecution_Returns404()
    {
        var (client, _) = await factory.CreateAuthenticatedClientAsync();

        var response = await client.GetAsync($"/api/content/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetHistory_IncludesGeneratedExecution()
    {
        var (client, campaign) = await CreateClientWithCampaignAsync();
        await client.PostAsJsonAsync("/api/content/generate", new GenerateContentRequest(campaign.Id, "AI-driven regulatory compliance tooling"), TestJsonOptions.Options);

        var response = await client.GetAsync($"/api/content/history?campaignId={campaign.Id}");

        var body = await response.Content.ReadFromJsonAsync<LinkedInContentManager.Contracts.Common.PagedResult<ContentHistoryItemResponse>>(TestJsonOptions.Options);
        body!.Items.Should().Contain(i => i.CampaignId == campaign.Id);
    }
}
