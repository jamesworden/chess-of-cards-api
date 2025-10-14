using System.Text.Json;

namespace ChessOfCards.Shared.Utilities;

/// <summary>
/// Provides helper methods for JSON serialization and deserialization.
/// </summary>
public static class JsonSerializationHelper
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    /// <summary>
    /// Deserializes an object to a specific type using JSON serialization round-trip.
    /// This is useful when you have a loosely-typed object (like from a JSON property)
    /// and need to convert it to a strongly-typed object.
    /// </summary>
    /// <typeparam name="T">The target type to deserialize to.</typeparam>
    /// <param name="data">The source data to deserialize.</param>
    /// <returns>The deserialized object of type T, or default(T) if data is null.</returns>
    public static T? DeserializeData<T>(object? data)
    {
        if (data == null)
            return default;

        return JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(data, Options), Options);
    }
}
