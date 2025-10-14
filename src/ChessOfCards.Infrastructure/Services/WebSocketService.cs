using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Amazon.ApiGatewayManagementApi;
using Amazon.ApiGatewayManagementApi.Model;
using Amazon.Runtime;

namespace ChessOfCards.Infrastructure.Services;

/// <summary>
/// Static hook for local testing to intercept WebSocket message sends.
/// </summary>
public static class LocalWebSocketHook
{
    public static Func<string, object, Task<bool>>? LocalSendMessage { get; set; }
}

/// <summary>
/// Service for managing WebSocket connections and sending messages
/// </summary>
public class WebSocketService
{
    private readonly IAmazonApiGatewayManagementApi _apiClient;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public WebSocketService(string websocketEndpoint)
    {
        // Check if we're in local testing mode
        var isLocalMode =
            websocketEndpoint.Contains("localhost")
            || websocketEndpoint.Contains("127.0.0.1");

        if (isLocalMode)
        {
            // In local mode, we'll use a dummy client since the LocalWebSocketServiceAdapter
            // will override the SendMessageAsync method
            _apiClient = null!;
        }
        else
        {
            var serviceUrl = websocketEndpoint.Replace("wss://", "https://");
            _apiClient = new AmazonApiGatewayManagementApiClient(
                new AmazonApiGatewayManagementApiConfig { ServiceURL = serviceUrl }
            );
        }
    }

    /// <summary>
    /// Send a message to a specific connection
    /// </summary>
    public virtual async Task<bool> SendMessageAsync(string connectionId, object message)
    {
        // Check if there's a local hook registered (for local testing)
        if (LocalWebSocketHook.LocalSendMessage != null)
        {
            Console.WriteLine($"[LOCAL] Using local WebSocket hook for {connectionId}");
            return await LocalWebSocketHook.LocalSendMessage(connectionId, message);
        }

        // Check if we're in local testing mode
        var isLocalTesting = Environment.GetEnvironmentVariable("IS_LOCAL_TESTING") == "true";
        if (isLocalTesting && _apiClient == null)
        {
            // In local mode, messages are handled by the local test server
            // The LocalWebSocketServiceAdapter will handle the actual sending
            Console.WriteLine($"[LOCAL] Message would be sent to {connectionId}: {JsonSerializer.Serialize(message, JsonOptions)}");
            return true;
        }

        try
        {
            var json = JsonSerializer.Serialize(message, JsonOptions);
            var bytes = Encoding.UTF8.GetBytes(json);

            var postRequest = new PostToConnectionRequest
            {
                ConnectionId = connectionId,
                Data = new MemoryStream(bytes),
            };

            await _apiClient.PostToConnectionAsync(postRequest);
            return true;
        }
        catch (GoneException)
        {
            // Connection no longer exists
            Console.WriteLine($"Connection {connectionId} is gone");
            return false;
        }
        catch (AmazonServiceException e)
        {
            Console.WriteLine($"Error sending message to {connectionId}: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// Send a message to multiple connections
    /// </summary>
    public virtual async Task<Dictionary<string, bool>> SendMessageToMultipleAsync(
        IEnumerable<string> connectionIds,
        object message
    )
    {
        var results = new Dictionary<string, bool>();
        var tasks = connectionIds.Select(async connectionId =>
        {
            var success = await SendMessageAsync(connectionId, message);
            return new KeyValuePair<string, bool>(connectionId, success);
        });

        var completedTasks = await Task.WhenAll(tasks);
        foreach (var result in completedTasks)
        {
            results[result.Key] = result.Value;
        }

        return results;
    }

    /// <summary>
    /// Check if a connection is still active
    /// </summary>
    public virtual async Task<bool> IsConnectionActiveAsync(string connectionId)
    {
        try
        {
            var request = new GetConnectionRequest { ConnectionId = connectionId };
            await _apiClient.GetConnectionAsync(request);
            return true;
        }
        catch (GoneException)
        {
            return false;
        }
        catch (AmazonServiceException)
        {
            return false;
        }
    }

    /// <summary>
    /// Disconnect a connection
    /// </summary>
    public virtual async Task<bool> DisconnectAsync(string connectionId)
    {
        try
        {
            var request = new DeleteConnectionRequest { ConnectionId = connectionId };
            await _apiClient.DeleteConnectionAsync(request);
            return true;
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error disconnecting {connectionId}: {e.Message}");
            return false;
        }
    }
}
