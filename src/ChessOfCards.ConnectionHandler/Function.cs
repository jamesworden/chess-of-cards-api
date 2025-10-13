using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using ChessOfCards.ConnectionHandler.Configuration;
using ChessOfCards.ConnectionHandler.Handlers;

[assembly: LambdaSerializer(
    typeof(ChessOfCards.Infrastructure.Serialization.CamelCaseLambdaJsonSerializer)
)]

namespace ChessOfCards.ConnectionHandler;

public class Function
{
    private readonly RouteDispatcher _routeDispatcher;

    public Function()
    {
        var services = ServiceConfiguration.ConfigureServices();
        _routeDispatcher = new RouteDispatcher(services);
    }

    public async Task<APIGatewayProxyResponse> FunctionHandler(
        APIGatewayProxyRequest request,
        ILambdaContext context
    )
    {
        try
        {
            context.Logger.LogInformation($"Route: {request.RequestContext.RouteKey}");
            context.Logger.LogInformation($"ConnectionId: {request.RequestContext.ConnectionId}");

            var routeKey = request.RequestContext.RouteKey;

            return await _routeDispatcher.DispatchAsync(routeKey, request, context);
        }
        catch (Exception ex)
        {
            context.Logger.LogError($"Error: {ex.Message}");
            context.Logger.LogError($"Stack trace: {ex.StackTrace}");
            return new APIGatewayProxyResponse { StatusCode = 500 };
        }
    }
}
