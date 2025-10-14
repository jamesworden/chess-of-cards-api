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
    // Static initialization - reused across Lambda invocations (container reuse)
    private static readonly ServiceDependencies Services = ServiceConfiguration.ConfigureServices();
    private static readonly RouteDispatcher RouteDispatcher = new RouteDispatcher(Services);

    public Function()
    {
        // Empty constructor - use static fields for Lambda container reuse
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

            return await RouteDispatcher.DispatchAsync(routeKey, request, context);
        }
        catch (Exception ex)
        {
            context.Logger.LogError($"Error: {ex.Message}");
            context.Logger.LogError($"Stack trace: {ex.StackTrace}");
            return new APIGatewayProxyResponse { StatusCode = 500 };
        }
    }
}
