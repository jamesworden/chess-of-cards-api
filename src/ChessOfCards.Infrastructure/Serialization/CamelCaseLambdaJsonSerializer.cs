using Amazon.Lambda.Serialization.SystemTextJson;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ChessOfCards.Infrastructure.Serialization;

/// <summary>
/// Custom Lambda JSON serializer that uses camelCase for property names.
/// This ensures consistent JSON property naming between C# (PascalCase) and JavaScript (camelCase).
/// </summary>
public class CamelCaseLambdaJsonSerializer : DefaultLambdaJsonSerializer
{
    public CamelCaseLambdaJsonSerializer()
        : base(CreateCustomizer())
    {
    }

    private static Action<JsonSerializerOptions> CreateCustomizer()
    {
        return options =>
        {
            // Use camelCase for all property names (C# PascalCase -> JSON camelCase)
            options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;

            // Allow reading property names in any case (case-insensitive)
            options.PropertyNameCaseInsensitive = true;

            // Ignore null values when serializing (cleaner JSON output)
            options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;

            // Pretty print for debugging (can be removed in production for smaller payloads)
            // options.WriteIndented = true;
        };
    }
}
