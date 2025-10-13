using System.Text.Json.Serialization;

namespace ChessOfCards.Infrastructure.Messages;

/// <summary>
/// Base class for WebSocket messages sent to clients
/// </summary>
public class WebSocketMessage
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public object? Data { get; set; }

    public WebSocketMessage()
    {
    }

    public WebSocketMessage(string type, object? data = null)
    {
        Type = type;
        Data = data;
    }
}

/// <summary>
/// Typed message for specific events
/// </summary>
public class WebSocketMessage<T> : WebSocketMessage
{
    [JsonPropertyName("data")]
    public new T? Data { get; set; }

    public WebSocketMessage()
    {
    }

    public WebSocketMessage(string type, T? data = default)
    {
        Type = type;
        Data = data;
    }
}

/// <summary>
/// Message types matching legacy SignalR commands
/// </summary>
public static class MessageTypes
{
    public const string CreatedPendingGame = "CreatedPendingGame";
    public const string GameStarted = "GameStarted";
    public const string GameUpdated = "GameUpdated";
    public const string GameOver = "GameOver";
    public const string OpponentDisconnected = "OpponentDisconnected";
    public const string OpponentReconnected = "OpponentReconnected";
    public const string PlayerReconnected = "PlayerReconnected";
    public const string ChatMessageSent = "ChatMessageSent";
    public const string DrawOffered = "DrawOffered";
    public const string TurnSkipped = "TurnSkipped";
    public const string JoinGameCodeInvalid = "JoinGameCodeInvalid";
    public const string GameNameInvalid = "GameNameInvalid";
    public const string LatestReadChatMessageMarked = "LatestReadChatMessageMarked";
    public const string Error = "Error";
    public const string Connected = "Connected";
}

/// <summary>
/// Incoming action request from client
/// </summary>
public class ActionRequest
{
    [JsonPropertyName("action")]
    public string Action { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public object? Data { get; set; }
}

/// <summary>
/// Incoming action request with typed data
/// </summary>
public class ActionRequest<T>
{
    [JsonPropertyName("action")]
    public string Action { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public T? Data { get; set; }
}
