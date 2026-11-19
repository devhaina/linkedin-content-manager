namespace LinkedInContentManager.IntegrationTests;

/// <summary>
/// Shares exactly one <see cref="CustomWebApplicationFactory"/> (and its one real Postgres
/// container) across every API test class. Five separate factories each starting their own
/// Postgres container concurrently is what originally caused every test to fail with
/// "password authentication failed" — not a credentials bug, but five Testcontainers Postgres
/// instances initializing at once under Docker Desktop resource contention. One shared
/// container, with xunit running this collection's test classes sequentially (the default for
/// classes in the same collection), is both faster and the correct fix.
/// </summary>
[CollectionDefinition("Api")]
public class ApiTestCollection : ICollectionFixture<CustomWebApplicationFactory>;
