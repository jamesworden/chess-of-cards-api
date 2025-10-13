using System.Text.Json;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using ChessOfCards.GameActionHandler.Configuration;
using ChessOfCards.GameActionHandler.Handlers;
using ChessOfCards.Infrastructure.Messages;
using ChessOfCards.Infrastructure.Services;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

[assembly: LambdaSerializer(
    typeof(ChessOfCards.Infrastructure.Serialization.CamelCaseLambdaJsonSerializer)
)]

namespace ChessOfCards.GameActionHandler;

public class Function
{
    private readonly ActionDispatcher _actionDispatcher;

    public Function()
    {
        var serviceProvider = ServiceConfiguration.ConfigureServices();
        var mediator = serviceProvider.GetRequiredService<IMediator>();
        var webSocketService = serviceProvider.GetRequiredService<WebSocketService>();

        _actionDispatcher = new ActionDispatcher(mediator, webSocketService);
    }

    public async Task<APIGatewayProxyResponse> FunctionHandler(
        APIGatewayProxyRequest request,
        ILambdaContext context
    )
    {
        try
        {
            var connectionId = request.RequestContext.ConnectionId;
            context.Logger.LogInformation($"ConnectionId: {connectionId}");
            context.Logger.LogInformation($"Body: {request.Body}");

            // Parse action from request body
            var actionRequest = JsonSerializer.Deserialize<ActionRequest>(request.Body);
            if (actionRequest == null || string.IsNullOrEmpty(actionRequest.Action))
            {
                context.Logger.LogWarning("Invalid action request");
                return new APIGatewayProxyResponse { StatusCode = 400 };
            }

            context.Logger.LogInformation($"Action: {actionRequest.Action}");

            // Dispatch to appropriate handler using ActionDispatcher
            await _actionDispatcher.DispatchAsync(
                actionRequest.Action,
                connectionId,
                actionRequest.Data,
                context
            );

            return new APIGatewayProxyResponse { StatusCode = 200 };
        }
        catch (Exception ex)
        {
            context.Logger.LogError($"Error: {ex.Message}");
            context.Logger.LogError($"Stack trace: {ex.StackTrace}");
            return new APIGatewayProxyResponse { StatusCode = 500 };
        }
    }
}
