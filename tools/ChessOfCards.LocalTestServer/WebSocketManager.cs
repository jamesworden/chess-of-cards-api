using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;

namespace ChessOfCards.LocalTestServer;

/// <summary>
/// Manages active WebSocket connections for local testing.
/// </summary>
public class LocalWebSocketManager
{
    private readonly ConcurrentDictionary<string, WebSocket> _connections = new();

    public void AddConnection(string connectionId, WebSocket webSocket)
    {
        _connections.TryAdd(connectionId, webSocket);
    }

    public void RemoveConnection(string connectionId)
    {
        _connections.TryRemove(connectionId, out _);
    }

    public async Task SendMessageAsync(string connectionId, string message)
    {
        if (_connections.TryGetValue(connectionId, out var webSocket))
        {
            if (webSocket.State == WebSocketState.Open)
            {
                var bytes = Encoding.UTF8.GetBytes(message);
                await webSocket.SendAsync(
                    new ArraySegment<byte>(bytes),
                    WebSocketMessageType.Text,
                    true,
                    CancellationToken.None
                );
            }
        }
    }

    public bool IsConnected(string connectionId)
    {
        return _connections.TryGetValue(connectionId, out var ws)
            && ws.State == WebSocketState.Open;
    }
}
