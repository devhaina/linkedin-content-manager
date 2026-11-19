using System.Text.Json;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace LinkedInContentManager.Infrastructure.Persistence.Converters;

/// <summary>
/// Persists a <see cref="List{String}"/> as a single <c>jsonb</c> column instead of a child
/// table. Used for small, order-sensitive, never-independently-queried collections like
/// research source URLs, hashtags, and critique recommendations.
/// </summary>
public sealed class JsonStringListConverter() : ValueConverter<List<string>, string>(
    list => JsonSerializer.Serialize(list, (JsonSerializerOptions?)null),
    json => string.IsNullOrWhiteSpace(json)
        ? new List<string>()
        : JsonSerializer.Deserialize<List<string>>(json, (JsonSerializerOptions?)null) ?? new List<string>())
{
    public static readonly ValueComparer<List<string>> Comparer = new(
        (a, b) => (a ?? new List<string>()).SequenceEqual(b ?? new List<string>()),
        v => v.Aggregate(0, (hash, item) => HashCode.Combine(hash, item.GetHashCode())),
        v => v.ToList());
}
