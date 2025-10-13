using Amazon.DynamoDBv2.DataModel;

namespace ChessOfCards.Infrastructure.Models;

/// <summary>
/// Represents a game timer (clock or disconnect timer)
/// </summary>
[DynamoDBTable("chess-of-cards-game-timers")]
public class GameTimerRecord
{
    [DynamoDBHashKey("timerId")]
    public string TimerId { get; set; } = string.Empty; // Format: "GAME#{gameCode}#CLOCK#{role}" or "DISCONNECT#{gameCode}#{role}"

    [DynamoDBProperty("gameCode")]
    public string GameCode { get; set; } = string.Empty;

    [DynamoDBProperty("timerType")]
    [DynamoDBGlobalSecondaryIndexHashKey("ExpiryIndex")]
    public string TimerType { get; set; } = string.Empty; // GAME_CLOCK_HOST, GAME_CLOCK_GUEST, DISCONNECT

    [DynamoDBProperty("playerRole")]
    public string? PlayerRole { get; set; } // HOST or GUEST

    [DynamoDBProperty("expiresAt")]
    [DynamoDBGlobalSecondaryIndexRangeKey("ExpiryIndex")]
    public long ExpiresAt { get; set; }

    [DynamoDBProperty("startedAt")]
    public long StartedAt { get; set; }

    [DynamoDBProperty("pausedAt")]
    public long? PausedAt { get; set; }

    [DynamoDBProperty("secondsElapsed")]
    public double SecondsElapsed { get; set; }

    [DynamoDBProperty("secondsRemaining")]
    public double SecondsRemaining { get; set; }

    [DynamoDBProperty("ttl")]
    public long Ttl { get; set; }

    public GameTimerRecord() { }

    public static GameTimerRecord CreateGameClock(
        string gameCode,
        string playerRole,
        double totalSeconds
    )
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        return new GameTimerRecord
        {
            TimerId = $"GAME#{gameCode}#CLOCK#{playerRole}",
            GameCode = gameCode,
            TimerType = $"GAME_CLOCK_{playerRole}",
            PlayerRole = playerRole,
            ExpiresAt = now + (long)totalSeconds,
            StartedAt = now,
            SecondsElapsed = 0,
            SecondsRemaining = totalSeconds,
            Ttl = now + (long)totalSeconds + 3600, // 1 hour buffer
        };
    }

    public static GameTimerRecord CreateDisconnectTimer(
        string gameCode,
        string playerRole,
        double gracePeriodSeconds = 30
    )
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        return new GameTimerRecord
        {
            TimerId = $"DISCONNECT#{gameCode}#{playerRole}",
            GameCode = gameCode,
            TimerType = "DISCONNECT",
            PlayerRole = playerRole,
            ExpiresAt = now + (long)gracePeriodSeconds,
            StartedAt = now,
            SecondsElapsed = 0,
            SecondsRemaining = gracePeriodSeconds,
            Ttl = now + (long)gracePeriodSeconds + 600, // 10 minute buffer
        };
    }
}
