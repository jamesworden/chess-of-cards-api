using Amazon.DynamoDBv2.DataModel;

namespace ChessOfCards.Infrastructure.Models;

/// <summary>
/// Represents a game lobby waiting for a second player
/// </summary>
[DynamoDBTable("chess-of-cards-pending-games")]
public class PendingGameRecord
{
    [DynamoDBHashKey("gameCode")]
    public string GameCode { get; set; } = string.Empty;

    [DynamoDBProperty("hostConnectionId")]
    [DynamoDBGlobalSecondaryIndexHashKey("HostConnectionIndex")]
    public string HostConnectionId { get; set; } = string.Empty;

    [DynamoDBProperty("hostName")]
    public string? HostName { get; set; }

    [DynamoDBProperty("durationOption")]
    public string DurationOption { get; set; } = "MEDIUM"; // SHORT, MEDIUM, LONG

    [DynamoDBProperty("createdAt")]
    [DynamoDBGlobalSecondaryIndexRangeKey("HostConnectionIndex")]
    public long CreatedAt { get; set; }

    [DynamoDBProperty("ttl")]
    public long Ttl { get; set; } // Auto-expire after 10 minutes

    public PendingGameRecord() { }

    public PendingGameRecord(
        string gameCode,
        string hostConnectionId,
        string durationOption,
        string? hostName
    )
    {
        GameCode = gameCode;
        HostConnectionId = hostConnectionId;
        DurationOption = durationOption;
        HostName = hostName;
        CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        Ttl = DateTimeOffset.UtcNow.AddMinutes(10).ToUnixTimeSeconds();
    }
}
