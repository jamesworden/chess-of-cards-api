using System.Text.Json;
using System.Text.Json.Serialization;

namespace ChessOfCards.Shared.Utilities;

/// <summary>
/// Provides standardized JSON serialization options for the entire application.
/// This ensures consistent serialization behavior across all services.
/// </summary>
public static class JsonOptions
{
    /// <summary>
    /// Default JSON serialization options used throughout the application.
    /// - Properties are serialized in camelCase
    /// - Enums are serialized as strings (e.g., "king" instead of 11)
    /// - Null values are ignored
    /// - Property names are case-insensitive when deserializing
    /// - Fields are included in serialization (required for domain models with private fields)
    /// </summary>
    public static readonly JsonSerializerOptions Default = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
        IncludeFields = true,
    };

    /// <summary>
    /// JSON serialization options with indented formatting for debugging.
    /// </summary>
    public static readonly JsonSerializerOptions Pretty = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
        WriteIndented = true,
        IncludeFields = true,
    };
}
