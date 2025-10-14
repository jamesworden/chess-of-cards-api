using ChessOfCards.Infrastructure.Services;

namespace ChessOfCards.LocalTestServer;

/// <summary>
/// Static registry for local testing services.
/// This allows Lambda functions to use local implementations during testing.
/// </summary>
public static class LocalServiceRegistry
{
    private static WebSocketService? _localWebSocketService;

    public static void RegisterWebSocketService(WebSocketService service)
    {
        _localWebSocketService = service;
    }

    public static WebSocketService? GetWebSocketService()
    {
        return _localWebSocketService;
    }

    public static void Clear()
    {
        _localWebSocketService = null;
    }
}
