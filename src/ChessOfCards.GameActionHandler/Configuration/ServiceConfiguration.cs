using Amazon.DynamoDBv2;
using Amazon.XRay.Recorder.Handlers.AwsSdk;
using ChessOfCards.GameActionHandler.Application.Features.Games.Commands;
using ChessOfCards.Infrastructure.Repositories;
using ChessOfCards.Infrastructure.Services;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ChessOfCards.GameActionHandler.Configuration;

/// <summary>
/// Configures dependency injection for the Lambda function.
/// </summary>
public static class ServiceConfiguration
{
    public static IServiceProvider ConfigureServices()
    {
        // Register X-Ray tracing for all AWS SDK calls
        AWSSDKHandler.RegisterXRayForAllServices();

        var services = new ServiceCollection();

        // Add logging
        services.AddLogging(builder => builder.AddConsole());

        // Register MediatR
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(CreatePendingGameCommand).Assembly)
        );

        // Get environment variables
        var connectionsTableName =
            Environment.GetEnvironmentVariable("CONNECTIONS_TABLE_NAME")
            ?? throw new Exception("CONNECTIONS_TABLE_NAME not set");
        var pendingGamesTableName =
            Environment.GetEnvironmentVariable("PENDING_GAMES_TABLE_NAME")
            ?? throw new Exception("PENDING_GAMES_TABLE_NAME not set");
        var activeGamesTableName =
            Environment.GetEnvironmentVariable("ACTIVE_GAMES_TABLE_NAME")
            ?? throw new Exception("ACTIVE_GAMES_TABLE_NAME not set");
        var gameTimersTableName =
            Environment.GetEnvironmentVariable("GAME_TIMERS_TABLE_NAME")
            ?? throw new Exception("GAME_TIMERS_TABLE_NAME not set");
        var websocketEndpoint =
            Environment.GetEnvironmentVariable("WEBSOCKET_ENDPOINT")
            ?? throw new Exception("WEBSOCKET_ENDPOINT not set");

        // Register AWS services with connection pooling
        services.AddSingleton<IAmazonDynamoDB>(_ =>
        {
            var config = new AmazonDynamoDBConfig
            {
                MaxConnectionsPerServer = 50, // Enable connection pooling for better performance
                Timeout = TimeSpan.FromSeconds(10), // Reasonable timeout for game operations
                MaxErrorRetry = 3 // Built-in retry logic for transient errors
            };
            return new AmazonDynamoDBClient(config);
        });

        // Register repositories
        services.AddScoped<IConnectionRepository>(sp => new ConnectionRepository(
            sp.GetRequiredService<IAmazonDynamoDB>(),
            connectionsTableName
        ));
        services.AddScoped<IPendingGameRepository>(sp => new PendingGameRepository(
            sp.GetRequiredService<IAmazonDynamoDB>(),
            pendingGamesTableName
        ));
        services.AddScoped<IActiveGameRepository>(sp => new ActiveGameRepository(
            sp.GetRequiredService<IAmazonDynamoDB>(),
            activeGamesTableName
        ));
        services.AddScoped<IGameTimerRepository>(sp => new GameTimerRepository(
            sp.GetRequiredService<IAmazonDynamoDB>(),
            gameTimersTableName
        ));

        // Register WebSocket service
        services.AddScoped(_ => new WebSocketService(websocketEndpoint));

        return services.BuildServiceProvider();
    }
}
