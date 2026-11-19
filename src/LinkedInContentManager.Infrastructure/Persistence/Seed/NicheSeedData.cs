using LinkedInContentManager.Domain.Entities;

namespace LinkedInContentManager.Infrastructure.Persistence.Seed;

/// <summary>
/// The five niches named in the brief, seeded as rows (not a hardcoded enum) so an operator
/// can add/retire a niche later without a deployment. IDs are fixed GUIDs so migrations are
/// deterministic and idempotent across environments.
/// </summary>
public static class NicheSeedData
{
    public static readonly Guid FintechId = Guid.Parse("11111111-0000-0000-0000-000000000001");
    public static readonly Guid FintechSecurityId = Guid.Parse("11111111-0000-0000-0000-000000000002");
    public static readonly Guid CryptoId = Guid.Parse("11111111-0000-0000-0000-000000000003");
    public static readonly Guid CryptocurrencyId = Guid.Parse("11111111-0000-0000-0000-000000000004");
    public static readonly Guid EmergingAiId = Guid.Parse("11111111-0000-0000-0000-000000000005");

    public static IEnumerable<Niche> GetSeedNiches()
    {
        yield return new Niche(FintechId, "Fintech", "fintech",
            "Financial technology: payments, banking infrastructure, lending, embedded finance.");
        yield return new Niche(FintechSecurityId, "Fintech Security", "fintech-security",
            "Security and fraud prevention specific to financial technology platforms.");
        yield return new Niche(CryptoId, "Crypto", "crypto",
            "Blockchain protocols, decentralized infrastructure, and crypto-native tooling.");
        yield return new Niche(CryptocurrencyId, "Cryptocurrency", "cryptocurrency",
            "Digital asset markets, exchanges, tokens, and adoption trends.");
        yield return new Niche(EmergingAiId, "Emerging AI Developments", "emerging-ai",
            "New developments in AI models, tooling, and enterprise adoption.");
    }
}
