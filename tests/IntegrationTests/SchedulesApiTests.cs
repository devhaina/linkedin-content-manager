using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using LinkedInContentManager.Contracts.Schedules;

namespace LinkedInContentManager.IntegrationTests;

[Collection("Api")]
public class SchedulesApiTests(CustomWebApplicationFactory factory)
{
    [Fact]
    public async Task Create_ForUnknownPost_Returns404()
    {
        var (client, _) = await factory.CreateAuthenticatedClientAsync();

        var response = await client.PostAsJsonAsync("/api/schedules",
            new CreateScheduleRequest(Guid.NewGuid(), DateTimeOffset.UtcNow.AddDays(1)), TestJsonOptions.Options);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_WithPastScheduledTime_Returns400()
    {
        var (client, _) = await factory.CreateAuthenticatedClientAsync();

        var response = await client.PostAsJsonAsync("/api/schedules",
            new CreateScheduleRequest(Guid.NewGuid(), DateTimeOffset.UtcNow.AddDays(-1)), TestJsonOptions.Options);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetAll_WithoutAuth_Returns401()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/schedules");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Delete_ForUnknownSchedule_Returns404()
    {
        var (client, _) = await factory.CreateAuthenticatedClientAsync();

        var response = await client.DeleteAsync($"/api/schedules/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
