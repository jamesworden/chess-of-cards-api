using ChessOfCards.Infrastructure.Services;

namespace ChessOfCards.LocalTestServer;

/// <summary>
/// Adapter that replaces the AWS API Gateway WebSocketService with local implementation.
/// This intercepts message sends and routes them through the LocalWebSocketManager instead.
/// </summary>
public class LocalWebSocketServiceAdapter : WebSocketService
{
    private readonly LocalWebSocketManager _localManager;
    private readonly ILogger<LocalWebSocketServiceAdapter> _logger;

    public LocalWebSocketServiceAdapter(
        LocalWebSocketManager localManager,
        ILogger<LocalWebSocketServiceAdapter> logger
    )
        : base("http://localhost:3001") // Dummy endpoint for base class
    {
        _localManager = localManager;
        _logger = logger;
    }

    public override async Task<bool> SendMessageAsync(string connectionId, object message)
    {
        try
        {
            var json = System.Text.Json.JsonSerializer.Serialize(
                message,
                new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
                    DefaultIgnoreCondition = System
                        .Text
                        .Json
                        .Serialization
                        .JsonIgnoreCondition
                        .WhenWritingNull,
                }
            );

            _logger.LogInformation($"[LOCAL] Sending to {connectionId}: {json}");
            await _localManager.SendMessageAsync(connectionId, json);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError($"[LOCAL] Error sending message to {connectionId}: {ex.Message}");
            return false;
        }
    }

    public override async Task<Dictionary<string, bool>> SendMessageToMultipleAsync(
        IEnumerable<string> connectionIds,
        object message
    )
    {
        var results = new Dictionary<string, bool>();
        foreach (var connectionId in connectionIds)
        {
            var success = await SendMessageAsync(connectionId, message);
            results[connectionId] = success;
        }
        return results;
    }

    public override Task<bool> IsConnectionActiveAsync(string connectionId)
    {
        return Task.FromResult(_localManager.IsConnected(connectionId));
    }

    public override Task<bool> DisconnectAsync(string connectionId)
    {
        _localManager.RemoveConnection(connectionId);
        return Task.FromResult(true);
    }
}
