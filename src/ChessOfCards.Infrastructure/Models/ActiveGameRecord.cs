using Amazon.DynamoDBv2.DataModel;
using ChessOfCards.Domain.Features.Games.Constants;

namespace ChessOfCards.Infrastructure.Models;

/// <summary>
/// Represents an active game with full state
/// </summary>
[DynamoDBTable("chess-of-cards-active-games")]
public class ActiveGameRecord
{
    [DynamoDBHashKey("gameCode")]
    public string GameCode { get; set; } = string.Empty;

    [DynamoDBProperty("hostConnectionId")]
    [DynamoDBGlobalSecondaryIndexHashKey("HostConnectionIndex")]
    public string HostConnectionId { get; set; } = string.Empty;

    [DynamoDBProperty("guestConnectionId")]
    [DynamoDBGlobalSecondaryIndexHashKey("GuestConnectionIndex")]
    public string GuestConnectionId { get; set; } = string.Empty;

    [DynamoDBProperty("hostName")]
    public string? HostName { get; set; }

    [DynamoDBProperty("guestName")]
    public string? GuestName { get; set; }

    [DynamoDBProperty("gameState")]
    public string GameState { get; set; } = "{}"; // JSON serialized Game object

    [DynamoDBProperty("isHostPlayersTurn")]
    public bool IsHostPlayersTurn { get; set; } = true;

    [DynamoDBProperty("hasEnded")]
    public bool HasEnded { get; set; } = false;

    [DynamoDBProperty("wonBy")]
    public string WonBy { get; set; } = "NONE"; // HOST, GUEST, NONE

    [DynamoDBProperty("durationOption")]
    public string DurationOption { get; set; } = DurationOptionConstants.Default;

    [DynamoDBProperty("createdAt")]
    [DynamoDBGlobalSecondaryIndexRangeKey("HostConnectionIndex", "GuestConnectionIndex")]
    public long CreatedAt { get; set; }

    [DynamoDBProperty("updatedAt")]
    public long UpdatedAt { get; set; }

    [DynamoDBProperty("version")]
    public int Version { get; set; } = 1;

    [DynamoDBProperty("hostDisconnectedAt")]
    public long? HostDisconnectedAt { get; set; }

    [DynamoDBProperty("guestDisconnectedAt")]
    public long? GuestDisconnectedAt { get; set; }

    [DynamoDBProperty("ttl")]
    public long Ttl { get; set; } // Auto-expire after 7 days

    public ActiveGameRecord() { }

    public ActiveGameRecord(
        string gameCode,
        string hostConnectionId,
        string guestConnectionId,
        string durationOption,
        string? hostName,
        string? guestName,
        string gameState
    )
    {
        GameCode = gameCode;
        HostConnectionId = hostConnectionId;
        GuestConnectionId = guestConnectionId;
        DurationOption = durationOption;
        HostName = hostName;
        GuestName = guestName;
        GameState = gameState;
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        CreatedAt = now;
        UpdatedAt = now;
        Ttl = DateTimeOffset.UtcNow.AddDays(7).ToUnixTimeSeconds();
    }
}
