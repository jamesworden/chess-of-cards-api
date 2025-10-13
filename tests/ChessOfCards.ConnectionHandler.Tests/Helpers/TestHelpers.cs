using Amazon.Lambda.APIGatewayEvents;

namespace ChessOfCards.ConnectionHandler.Tests.Helpers;

/// <summary>
/// Helper methods for creating test data.
/// </summary>
public static class TestHelpers
{
    public static APIGatewayProxyRequest CreateConnectRequest(string connectionId)
    {
        return new APIGatewayProxyRequest
        {
            RequestContext = new APIGatewayProxyRequest.ProxyRequestContext
            {
                RouteKey = "$connect",
                ConnectionId = connectionId,
                ApiId = "test-api-id",
                Stage = "dev",
                RequestId = Guid.NewGuid().ToString()
            }
        };
    }

    public static APIGatewayProxyRequest CreateDisconnectRequest(string connectionId)
    {
        return new APIGatewayProxyRequest
        {
            RequestContext = new APIGatewayProxyRequest.ProxyRequestContext
            {
                RouteKey = "$disconnect",
                ConnectionId = connectionId,
                ApiId = "test-api-id",
                Stage = "dev",
                RequestId = Guid.NewGuid().ToString()
            }
        };
    }
}
