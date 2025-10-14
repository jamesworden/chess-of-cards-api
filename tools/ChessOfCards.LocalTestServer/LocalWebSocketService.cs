namespace ChessOfCards.LocalTestServer;

/// <summary>
/// Local implementation of WebSocket service that uses the WebSocketManager
/// instead of API Gateway Management API.
/// </summary>
public class LocalWebSocketService
{
    private readonly LocalWebSocketManager _webSocketManager;
    private readonly ILogger<LocalWebSocketService> _logger;

    public LocalWebSocketService(
        LocalWebSocketManager webSocketManager,
        ILogger<LocalWebSocketService> logger
    )
    {
        _webSocketManager = webSocketManager;
        _logger = logger;
    }

    public async Task SendMessageAsync(string connectionId, string message)
    {
        try
        {
            await _webSocketManager.SendMessageAsync(connectionId, message);
            _logger.LogInformation($"Sent message to {connectionId}: {message}");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error sending message to {connectionId}: {ex.Message}");
        }
    }
}
