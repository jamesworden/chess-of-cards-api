using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Amazon.Lambda.APIGatewayEvents;

namespace ChessOfCards.LocalTestServer;

/// <summary>
/// Handles WebSocket connections and routes them to Lambda functions.
/// </summary>
public class WebSocketHandler
{
    private readonly LocalWebSocketManager _webSocketManager;
    private readonly ILogger<WebSocketHandler> _logger;
    private readonly ConnectionHandler.Function _connectionHandler;
    private readonly GameActionHandler.Function _gameActionHandler;
    private readonly LocalWebSocketServiceAdapter _localWebSocketService;

    public WebSocketHandler(
        LocalWebSocketManager webSocketManager,
        ILogger<WebSocketHandler> logger,
        LocalWebSocketServiceAdapter localWebSocketService
    )
    {
        _webSocketManager = webSocketManager;
        _logger = logger;
        _localWebSocketService = localWebSocketService;

        // Register the local hook so Lambda functions can send messages through our local manager
        Infrastructure.Services.LocalWebSocketHook.LocalSendMessage = async (connectionId, message) =>
        {
            _logger.LogInformation($"[LOCAL HOOK] Sending message to {connectionId}");
            return await _localWebSocketService.SendMessageAsync(connectionId, message);
        };

        _connectionHandler = new ConnectionHandler.Function();
        _gameActionHandler = new GameActionHandler.Function();
    }

    public async Task HandleWebSocketAsync(HttpContext context)
    {
        if (!context.WebSockets.IsWebSocketRequest)
        {
            context.Response.StatusCode = 400;
            return;
        }

        var webSocket = await context.WebSockets.AcceptWebSocketAsync();
        var connectionId = Guid.NewGuid().ToString();

        _logger.LogInformation($"WebSocket connection established: {connectionId}");
        _webSocketManager.AddConnection(connectionId, webSocket);

        try
        {
            // Handle $connect route
            await HandleConnectAsync(connectionId);

            // Handle messages
            await ReceiveMessagesAsync(webSocket, connectionId);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error handling WebSocket {connectionId}: {ex.Message}");
        }
        finally
        {
            // Handle $disconnect route
            await HandleDisconnectAsync(connectionId);
            _webSocketManager.RemoveConnection(connectionId);

            if (webSocket.State == WebSocketState.Open)
            {
                await webSocket.CloseAsync(
                    WebSocketCloseStatus.NormalClosure,
                    "Connection closed",
                    CancellationToken.None
                );
            }

            _logger.LogInformation($"WebSocket connection closed: {connectionId}");
        }
    }

    private async Task HandleConnectAsync(string connectionId)
    {
        var request = new APIGatewayProxyRequest
        {
            RequestContext = new APIGatewayProxyRequest.ProxyRequestContext
            {
                ConnectionId = connectionId,
                RouteKey = "$connect",
            },
        };

        var lambdaContext = new LocalLambdaContext(_logger);
        await _connectionHandler.FunctionHandler(request, lambdaContext);
    }

    private async Task HandleDisconnectAsync(string connectionId)
    {
        var request = new APIGatewayProxyRequest
        {
            RequestContext = new APIGatewayProxyRequest.ProxyRequestContext
            {
                ConnectionId = connectionId,
                RouteKey = "$disconnect",
            },
        };

        var lambdaContext = new LocalLambdaContext(_logger);
        await _connectionHandler.FunctionHandler(request, lambdaContext);
    }

    private async Task ReceiveMessagesAsync(WebSocket webSocket, string connectionId)
    {
        var buffer = new byte[1024 * 4];

        while (webSocket.State == WebSocketState.Open)
        {
            var result = await webSocket.ReceiveAsync(
                new ArraySegment<byte>(buffer),
                CancellationToken.None
            );

            if (result.MessageType == WebSocketMessageType.Close)
            {
                break;
            }

            if (result.MessageType == WebSocketMessageType.Text)
            {
                var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                _logger.LogInformation($"Received message from {connectionId}: {message}");

                await HandleMessageAsync(connectionId, message);
            }
        }
    }

    private async Task HandleMessageAsync(string connectionId, string message)
    {
        try
        {
            // Parse the action from the message
            var jsonDoc = JsonDocument.Parse(message);
            var action = jsonDoc.RootElement.GetProperty("action").GetString() ?? "$default";

            // Create Lambda request
            var request = new APIGatewayProxyRequest
            {
                RequestContext = new APIGatewayProxyRequest.ProxyRequestContext
                {
                    ConnectionId = connectionId,
                    RouteKey = action == "$default" ? "$default" : "$default",
                },
                Body = message,
            };

            var lambdaContext = new LocalLambdaContext(_logger);

            // Route to GameActionHandler for all message actions
            await _gameActionHandler.FunctionHandler(request, lambdaContext);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error handling message: {ex.Message}");

            // Send error back to client
            var errorMessage = JsonSerializer.Serialize(
                new { type = "Error", data = new { error = "Failed to process message" } }
            );

            await _webSocketManager.SendMessageAsync(connectionId, errorMessage);
        }
    }
}
