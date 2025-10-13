using Amazon.DynamoDBv2;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using ChessOfCards.Infrastructure.Messages;
using ChessOfCards.Infrastructure.Models;
using ChessOfCards.Infrastructure.Repositories;
using ChessOfCards.Infrastructure.Services;
using System.Text.Json;

[assembly: LambdaSerializer(typeof(ChessOfCards.Infrastructure.Serialization.CamelCaseLambdaJsonSerializer))]

namespace ChessOfCards.GameActionHandler;

public class Function
{
    private readonly IAmazonDynamoDB _dynamoDbClient;
    private readonly IConnectionRepository _connectionRepository;
    private readonly IPendingGameRepository _pendingGameRepository;
    private readonly IActiveGameRepository _activeGameRepository;
    private readonly IGameTimerRepository _timerRepository;
    private readonly WebSocketService _webSocketService;

    public Function()
    {
        _dynamoDbClient = new AmazonDynamoDBClient();

        var connectionsTableName = Environment.GetEnvironmentVariable("CONNECTIONS_TABLE_NAME")
            ?? throw new Exception("CONNECTIONS_TABLE_NAME not set");
        var pendingGamesTableName = Environment.GetEnvironmentVariable("PENDING_GAMES_TABLE_NAME")
            ?? throw new Exception("PENDING_GAMES_TABLE_NAME not set");
        var activeGamesTableName = Environment.GetEnvironmentVariable("ACTIVE_GAMES_TABLE_NAME")
            ?? throw new Exception("ACTIVE_GAMES_TABLE_NAME not set");
        var gameTimersTableName = Environment.GetEnvironmentVariable("GAME_TIMERS_TABLE_NAME")
            ?? throw new Exception("GAME_TIMERS_TABLE_NAME not set");
        var websocketEndpoint = Environment.GetEnvironmentVariable("WEBSOCKET_ENDPOINT")
            ?? throw new Exception("WEBSOCKET_ENDPOINT not set");

        _connectionRepository = new ConnectionRepository(_dynamoDbClient, connectionsTableName);
        _pendingGameRepository = new PendingGameRepository(_dynamoDbClient, pendingGamesTableName);
        _activeGameRepository = new ActiveGameRepository(_dynamoDbClient, activeGamesTableName);
        _timerRepository = new GameTimerRepository(_dynamoDbClient, gameTimersTableName);
        _webSocketService = new WebSocketService(websocketEndpoint);
    }

    public async Task<APIGatewayProxyResponse> FunctionHandler(
        APIGatewayProxyRequest request,
        ILambdaContext context)
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

            // Route to appropriate handler
            var response = actionRequest.Action switch
            {
                "createPendingGame" => await HandleCreatePendingGameAsync(connectionId, actionRequest.Data, context),
                "joinGame" => await HandleJoinGameAsync(connectionId, actionRequest.Data, context),
                "deletePendingGame" => await HandleDeletePendingGameAsync(connectionId, context),
                _ => await HandleUnknownActionAsync(connectionId, actionRequest.Action, context)
            };

            return response;
        }
        catch (Exception ex)
        {
            context.Logger.LogError($"Error: {ex.Message}");
            context.Logger.LogError($"Stack trace: {ex.StackTrace}");
            return new APIGatewayProxyResponse { StatusCode = 500 };
        }
    }

    private async Task<APIGatewayProxyResponse> HandleCreatePendingGameAsync(
        string connectionId,
        object? data,
        ILambdaContext context)
    {
        try
        {
            // Parse request data
            var requestData = JsonSerializer.Deserialize<CreatePendingGameRequest>(
                JsonSerializer.Serialize(data)
            );

            if (requestData == null)
            {
                await SendErrorAsync(connectionId, "Invalid request data");
                return new APIGatewayProxyResponse { StatusCode = 200 };
            }

            context.Logger.LogInformation($"Creating pending game for {connectionId}");

            // Validate game name
            if (!string.IsNullOrWhiteSpace(requestData.HostName) &&
                ContainsBadWords(requestData.HostName))
            {
                await _webSocketService.SendMessageAsync(connectionId, new WebSocketMessage(
                    MessageTypes.GameNameInvalid,
                    new { reason = "Name contains inappropriate content" }
                ));
                return new APIGatewayProxyResponse { StatusCode = 200 };
            }

            // Generate game code (6 character alphanumeric)
            var gameCode = GenerateGameCode();

            // Create pending game record
            var pendingGame = new PendingGameRecord(
                gameCode,
                connectionId,
                requestData.DurationOption ?? "MEDIUM",
                requestData.HostName
            );

            await _pendingGameRepository.CreateAsync(pendingGame);

            // Update connection record
            var connection = await _connectionRepository.GetByConnectionIdAsync(connectionId);
            if (connection != null)
            {
                connection.GameCode = gameCode;
                connection.PlayerRole = "HOST";
                connection.PlayerName = requestData.HostName;
                await _connectionRepository.UpdateAsync(connection);
            }

            context.Logger.LogInformation($"Created pending game {gameCode}");

            // Send response to client
            await _webSocketService.SendMessageAsync(connectionId, new WebSocketMessage(
                MessageTypes.CreatedPendingGame,
                new
                {
                    gameCode,
                    durationOption = pendingGame.DurationOption,
                    hostName = pendingGame.HostName
                }
            ));

            return new APIGatewayProxyResponse { StatusCode = 200 };
        }
        catch (Exception ex)
        {
            context.Logger.LogError($"Error creating pending game: {ex.Message}");
            await SendErrorAsync(connectionId, "Failed to create game");
            return new APIGatewayProxyResponse { StatusCode = 200 };
        }
    }

    private async Task<APIGatewayProxyResponse> HandleJoinGameAsync(
        string connectionId,
        object? data,
        ILambdaContext context)
    {
        try
        {
            // Parse request data
            var requestData = JsonSerializer.Deserialize<JoinGameRequest>(
                JsonSerializer.Serialize(data)
            );

            if (requestData == null || string.IsNullOrWhiteSpace(requestData.GameCode))
            {
                await SendErrorAsync(connectionId, "Invalid request data");
                return new APIGatewayProxyResponse { StatusCode = 200 };
            }

            context.Logger.LogInformation($"Player joining game {requestData.GameCode}");

            // Validate guest name
            if (!string.IsNullOrWhiteSpace(requestData.GuestName) &&
                ContainsBadWords(requestData.GuestName))
            {
                await _webSocketService.SendMessageAsync(connectionId, new WebSocketMessage(
                    MessageTypes.GameNameInvalid,
                    new { reason = "Name contains inappropriate content" }
                ));
                return new APIGatewayProxyResponse { StatusCode = 200 };
            }

            // Find pending game
            var pendingGame = await _pendingGameRepository.GetByGameCodeAsync(requestData.GameCode.ToUpper());
            if (pendingGame == null)
            {
                await _webSocketService.SendMessageAsync(connectionId, new WebSocketMessage(
                    MessageTypes.JoinGameCodeInvalid,
                    new { gameCode = requestData.GameCode }
                ));
                return new APIGatewayProxyResponse { StatusCode = 200 };
            }

            // Can't join your own game
            if (pendingGame.HostConnectionId == connectionId)
            {
                await SendErrorAsync(connectionId, "Cannot join your own game");
                return new APIGatewayProxyResponse { StatusCode = 200 };
            }

            context.Logger.LogInformation($"Starting game {pendingGame.GameCode}");

            // Create active game (simplified - full game logic will be added later)
            var activeGame = new ActiveGameRecord(
                pendingGame.GameCode,
                pendingGame.HostConnectionId,
                connectionId,
                pendingGame.DurationOption,
                pendingGame.HostName,
                requestData.GuestName,
                "{}" // Empty game state for now - will implement game initialization
            );

            await _activeGameRepository.CreateAsync(activeGame);

            // Delete pending game
            await _pendingGameRepository.DeleteAsync(pendingGame.GameCode);

            // Update guest connection
            var guestConnection = await _connectionRepository.GetByConnectionIdAsync(connectionId);
            if (guestConnection != null)
            {
                guestConnection.GameCode = pendingGame.GameCode;
                guestConnection.PlayerRole = "GUEST";
                guestConnection.PlayerName = requestData.GuestName;
                await _connectionRepository.UpdateAsync(guestConnection);
            }

            context.Logger.LogInformation($"Game {pendingGame.GameCode} started");

            // Notify both players
            var gameStartedMessage = new WebSocketMessage(
                MessageTypes.GameStarted,
                new
                {
                    gameCode = activeGame.GameCode,
                    hostName = activeGame.HostName,
                    guestName = activeGame.GuestName,
                    durationOption = activeGame.DurationOption,
                    isHostPlayersTurn = activeGame.IsHostPlayersTurn
                }
            );

            await _webSocketService.SendMessageAsync(pendingGame.HostConnectionId, gameStartedMessage);
            await _webSocketService.SendMessageAsync(connectionId, gameStartedMessage);

            return new APIGatewayProxyResponse { StatusCode = 200 };
        }
        catch (Exception ex)
        {
            context.Logger.LogError($"Error joining game: {ex.Message}");
            await SendErrorAsync(connectionId, "Failed to join game");
            return new APIGatewayProxyResponse { StatusCode = 200 };
        }
    }

    private async Task<APIGatewayProxyResponse> HandleDeletePendingGameAsync(
        string connectionId,
        ILambdaContext context)
    {
        try
        {
            context.Logger.LogInformation($"Deleting pending game for {connectionId}");

            // Find pending game by host connection
            var pendingGame = await _pendingGameRepository.GetByHostConnectionIdAsync(connectionId);
            if (pendingGame == null)
            {
                await SendErrorAsync(connectionId, "No pending game found");
                return new APIGatewayProxyResponse { StatusCode = 200 };
            }

            // Delete pending game
            await _pendingGameRepository.DeleteAsync(pendingGame.GameCode);

            // Update connection record
            var connection = await _connectionRepository.GetByConnectionIdAsync(connectionId);
            if (connection != null)
            {
                connection.GameCode = null;
                connection.PlayerRole = null;
                await _connectionRepository.UpdateAsync(connection);
            }

            context.Logger.LogInformation($"Deleted pending game {pendingGame.GameCode}");

            return new APIGatewayProxyResponse { StatusCode = 200 };
        }
        catch (Exception ex)
        {
            context.Logger.LogError($"Error deleting pending game: {ex.Message}");
            await SendErrorAsync(connectionId, "Failed to delete game");
            return new APIGatewayProxyResponse { StatusCode = 200 };
        }
    }

    private async Task<APIGatewayProxyResponse> HandleUnknownActionAsync(
        string connectionId,
        string action,
        ILambdaContext context)
    {
        context.Logger.LogWarning($"Unknown action: {action}");
        await SendErrorAsync(connectionId, $"Unknown action: {action}");
        return new APIGatewayProxyResponse { StatusCode = 200 };
    }

    private async Task SendErrorAsync(string connectionId, string message)
    {
        await _webSocketService.SendMessageAsync(connectionId, new WebSocketMessage(
            MessageTypes.Error,
            new { error = message }
        ));
    }

    private string GenerateGameCode()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // Exclude similar looking characters
        var random = new Random();
        return new string(Enumerable.Repeat(chars, 6)
            .Select(s => s[random.Next(s.Length)])
            .ToArray());
    }

    private bool ContainsBadWords(string text)
    {
        // Simplified bad word check - in production, use a proper profanity filter
        var badWords = new[] { "badword1", "badword2" }; // Placeholder
        return badWords.Any(word => text.Contains(word, StringComparison.OrdinalIgnoreCase));
    }
}

// Request DTOs
public class CreatePendingGameRequest
{
    public string? DurationOption { get; set; }
    public string? HostName { get; set; }
}

public class JoinGameRequest
{
    public string GameCode { get; set; } = string.Empty;
    public string? GuestName { get; set; }
}
