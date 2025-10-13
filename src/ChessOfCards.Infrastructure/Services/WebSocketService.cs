using Amazon.ApiGatewayManagementApi;
using Amazon.ApiGatewayManagementApi.Model;
using Amazon.Runtime;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ChessOfCards.Infrastructure.Services;

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
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public WebSocketService(string websocketEndpoint)
    {
        var serviceUrl = websocketEndpoint.Replace("wss://", "https://");
        _apiClient = new AmazonApiGatewayManagementApiClient(
            new AmazonApiGatewayManagementApiConfig
            {
                ServiceURL = serviceUrl
            }
        );
    }

    /// <summary>
    /// Send a message to a specific connection
    /// </summary>
    public async Task<bool> SendMessageAsync(string connectionId, object message)
    {
        try
        {
            var json = JsonSerializer.Serialize(message, JsonOptions);
            var bytes = Encoding.UTF8.GetBytes(json);

            var postRequest = new PostToConnectionRequest
            {
                ConnectionId = connectionId,
                Data = new MemoryStream(bytes)
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
    public async Task<Dictionary<string, bool>> SendMessageToMultipleAsync(
        IEnumerable<string> connectionIds,
        object message)
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
    public async Task<bool> IsConnectionActiveAsync(string connectionId)
    {
        try
        {
            var request = new GetConnectionRequest
            {
                ConnectionId = connectionId
            };
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
    public async Task<bool> DisconnectAsync(string connectionId)
    {
        try
        {
            var request = new DeleteConnectionRequest
            {
                ConnectionId = connectionId
            };
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
