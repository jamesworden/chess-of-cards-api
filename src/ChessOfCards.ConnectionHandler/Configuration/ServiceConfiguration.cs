using Amazon.DynamoDBv2;
using Amazon.XRay.Recorder.Handlers.AwsSdk;
using ChessOfCards.Infrastructure.Repositories;
using ChessOfCards.Infrastructure.Services;

namespace ChessOfCards.ConnectionHandler.Configuration;

/// <summary>
/// Configures dependencies for the ConnectionHandler Lambda function.
/// </summary>
public static class ServiceConfiguration
{
    public static ServiceDependencies ConfigureServices()
    {
        // Register X-Ray tracing for all AWS SDK calls
        AWSSDKHandler.RegisterXRayForAllServices();

        // Configure DynamoDB client with connection pooling for better performance
        var config = new AmazonDynamoDBConfig
        {
            MaxConnectionsPerServer = 50, // Enable connection pooling for better performance
            Timeout = TimeSpan.FromSeconds(10), // Reasonable timeout for connection operations
            MaxErrorRetry = 3, // Built-in retry logic for transient errors
        };
        var dynamoDbClient = new AmazonDynamoDBClient(config);

        // Get environment variables
        var connectionsTableName =
            Environment.GetEnvironmentVariable("CONNECTIONS_TABLE_NAME")
            ?? throw new Exception("CONNECTIONS_TABLE_NAME not set");
        var activeGamesTableName =
            Environment.GetEnvironmentVariable("ACTIVE_GAMES_TABLE_NAME")
            ?? throw new Exception("ACTIVE_GAMES_TABLE_NAME not set");
        var gameTimersTableName =
            Environment.GetEnvironmentVariable("GAME_TIMERS_TABLE_NAME")
            ?? throw new Exception("GAME_TIMERS_TABLE_NAME not set");
        var websocketEndpoint =
            Environment.GetEnvironmentVariable("WEBSOCKET_ENDPOINT")
            ?? throw new Exception("WEBSOCKET_ENDPOINT not set");

        // Create repositories and services
        var connectionRepository = new ConnectionRepository(dynamoDbClient, connectionsTableName);
        var gameRepository = new ActiveGameRepository(dynamoDbClient, activeGamesTableName);
        var timerRepository = new GameTimerRepository(dynamoDbClient, gameTimersTableName);
        var webSocketService = new WebSocketService(websocketEndpoint);

        return new ServiceDependencies(
            connectionRepository,
            gameRepository,
            timerRepository,
            webSocketService
        );
    }
}

/// <summary>
/// Container for all service dependencies.
/// </summary>
public record ServiceDependencies(
    IConnectionRepository ConnectionRepository,
    IActiveGameRepository GameRepository,
    IGameTimerRepository TimerRepository,
    WebSocketService WebSocketService
);
