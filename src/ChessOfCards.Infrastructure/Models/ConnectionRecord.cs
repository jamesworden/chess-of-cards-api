using Amazon.DynamoDBv2.DataModel;

namespace ChessOfCards.Infrastructure.Models;

/// <summary>
/// Represents an active WebSocket connection in DynamoDB
/// </summary>
[DynamoDBTable("chess-of-cards-connections")]
public class ConnectionRecord
{
    [DynamoDBHashKey("connectionId")]
    public string ConnectionId { get; set; } = string.Empty;

    [DynamoDBProperty("gameCode")]
    [DynamoDBGlobalSecondaryIndexHashKey("GameCodeIndex")]
    public string? GameCode { get; set; }

    [DynamoDBProperty("playerRole")]
    public string? PlayerRole { get; set; } // "HOST" or "GUEST"

    [DynamoDBProperty("playerName")]
    public string? PlayerName { get; set; }

    [DynamoDBProperty("connectedAt")]
    public long ConnectedAt { get; set; }

    [DynamoDBProperty("ttl")]
    public long Ttl { get; set; } // Auto-expire after 24 hours

    public ConnectionRecord() { }

    public ConnectionRecord(string connectionId, string? playerName = null)
    {
        ConnectionId = connectionId;
        PlayerName = playerName;
        ConnectedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        Ttl = DateTimeOffset.UtcNow.AddHours(24).ToUnixTimeSeconds();
    }
}
