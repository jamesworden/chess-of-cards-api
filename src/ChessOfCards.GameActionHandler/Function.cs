using System.Text.Json;
using Amazon.DynamoDBv2;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using ChessOfCards.GameActionHandler.Application.Features.Games.Commands;
using ChessOfCards.GameActionHandler.Requests;
using ChessOfCards.GameActionHandler.Validators;
using ChessOfCards.Infrastructure.Messages;
using ChessOfCards.Infrastructure.Repositories;
using ChessOfCards.Infrastructure.Services;
using ChessOfCards.Shared.Utilities;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

[assembly: LambdaSerializer(
    typeof(ChessOfCards.Infrastructure.Serialization.CamelCaseLambdaJsonSerializer)
)]

namespace ChessOfCards.GameActionHandler;

public class Function
{
    private readonly IMediator _mediator;
    private readonly WebSocketService _webSocketService;

    public Function()
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        var serviceProvider = services.BuildServiceProvider();

        _mediator = serviceProvider.GetRequiredService<IMediator>();
        _webSocketService = serviceProvider.GetRequiredService<WebSocketService>();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
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

        // Register AWS services
        services.AddSingleton<IAmazonDynamoDB>(_ => new AmazonDynamoDBClient());

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

            // Dispatch to appropriate handler using MediatR
            await DispatchActionAsync(
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

    private async Task DispatchActionAsync(
        string action,
        string connectionId,
        object? data,
        ILambdaContext context
    )
    {
        switch (action)
        {
            case "createPendingGame":
                await HandleCreatePendingGameActionAsync(connectionId, data);
                break;

            case "joinGame":
                await HandleJoinGameActionAsync(connectionId, data);
                break;

            case "deletePendingGame":
                await HandleDeletePendingGameActionAsync(connectionId);
                break;

            default:
                await HandleUnknownActionAsync(connectionId, action, context);
                break;
        }
    }

    private async Task HandleCreatePendingGameActionAsync(string connectionId, object? data)
    {
        var createRequest = JsonSerializationHelper.DeserializeData<CreatePendingGameRequest>(data);
        if (createRequest == null)
        {
            await SendErrorAsync(connectionId, "Invalid request data");
            return;
        }

        var validator = new CreatePendingGameRequestValidator();
        var validationResult = await validator.ValidateAsync(createRequest);

        if (!validationResult.IsValid)
        {
            var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
            await SendErrorAsync(connectionId, $"Validation failed: {errors}");
            return;
        }

        await _mediator.Send(createRequest.ToCommand(connectionId));
    }

    private async Task HandleJoinGameActionAsync(string connectionId, object? data)
    {
        var joinRequest = JsonSerializationHelper.DeserializeData<JoinGameRequest>(data);
        if (joinRequest == null)
        {
            await SendErrorAsync(connectionId, "Invalid request data");
            return;
        }

        var validator = new JoinGameRequestValidator();
        var validationResult = await validator.ValidateAsync(joinRequest);

        if (!validationResult.IsValid)
        {
            var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
            await SendErrorAsync(connectionId, $"Validation failed: {errors}");
            return;
        }

        await _mediator.Send(joinRequest.ToCommand(connectionId));
    }

    private async Task HandleDeletePendingGameActionAsync(string connectionId)
    {
        await _mediator.Send(new DeletePendingGameCommand(connectionId));
    }

    private async Task HandleUnknownActionAsync(
        string connectionId,
        string action,
        ILambdaContext context
    )
    {
        context.Logger.LogWarning($"Unknown action: {action}");
        await SendErrorAsync(connectionId, $"Unknown action: {action}");
    }

    private async Task SendErrorAsync(string connectionId, string message)
    {
        await _webSocketService.SendMessageAsync(
            connectionId,
            new WebSocketMessage(MessageTypes.Error, new { error = message })
        );
    }
}
