using System.Reflection;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using ChessOfCards.Infrastructure.Services;

namespace ChessOfCards.LocalTestServer;

/// <summary>
/// Wraps the ConnectionHandler function and injects our local WebSocket service.
/// </summary>
public class LocalConnectionHandlerWrapper
{
    private readonly ConnectionHandler.Function _function;
    private readonly LocalWebSocketServiceAdapter _localService;
    private readonly ILogger _logger;

    public LocalConnectionHandlerWrapper(
        LocalWebSocketServiceAdapter localService,
        ILogger logger
    )
    {
        _function = new ConnectionHandler.Function();
        _localService = localService;
        _logger = logger;
    }

    public async Task<APIGatewayProxyResponse> InvokeAsync(
        APIGatewayProxyRequest request,
        ILambdaContext context
    )
    {
        // Replace the WebSocketService in the function with our local one
        InjectLocalService(_function);

        return await _function.FunctionHandler(request, context);
    }

    private void InjectLocalService(object function)
    {
        try
        {
            // Use reflection to replace the WebSocketService
            var type = function.GetType();
            var field = type.GetField(
                "_webSocketService",
                BindingFlags.NonPublic | BindingFlags.Instance
            );

            if (field != null)
            {
                field.SetValue(function, _localService);
                _logger.LogInformation("[LOCAL] Injected local WebSocket service");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"[LOCAL] Could not inject service: {ex.Message}");
        }
    }
}
