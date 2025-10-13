using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using ChessOfCards.ConnectionHandler.Configuration;
using ChessOfCards.Infrastructure.Messages;
using ChessOfCards.Infrastructure.Models;

namespace ChessOfCards.ConnectionHandler.Handlers;

/// <summary>
/// Dispatches WebSocket route events to appropriate handlers.
/// </summary>
public class RouteDispatcher
{
    private readonly ServiceDependencies _services;

    public RouteDispatcher(ServiceDependencies services)
    {
        _services = services;
    }

    public async Task<APIGatewayProxyResponse> DispatchAsync(
        string routeKey,
        APIGatewayProxyRequest request,
        ILambdaContext context
    )
    {
        return routeKey switch
        {
            "$connect" => await HandleConnectAsync(request, context),
            "$disconnect" => await HandleDisconnectAsync(request, context),
            _ => new APIGatewayProxyResponse { StatusCode = 400 },
        };
    }

    private async Task<APIGatewayProxyResponse> HandleConnectAsync(
        APIGatewayProxyRequest request,
        ILambdaContext context
    )
    {
        var connectionId = request.RequestContext.ConnectionId;
        context.Logger.LogInformation($"New connection: {connectionId}");

        try
        {
            // Check for reconnection - is there a disconnected game for this player?
            var existingGame = await TryReconnectPlayerAsync(connectionId, context);

            if (existingGame != null)
            {
                context.Logger.LogInformation(
                    $"Player reconnected to game {existingGame.GameCode}"
                );

                // Send reconnection success message
                await _services.WebSocketService.SendMessageAsync(
                    connectionId,
                    new WebSocketMessage(
                        MessageTypes.PlayerReconnected,
                        new { game = existingGame }
                    )
                );
            }
            else
            {
                // New connection - create connection record
                var connection = new ConnectionRecord(connectionId);
                await _services.ConnectionRepository.CreateAsync(connection);

                context.Logger.LogInformation($"Created connection record for {connectionId}");

                // Send connected message
                await _services.WebSocketService.SendMessageAsync(
                    connectionId,
                    new WebSocketMessage(MessageTypes.Connected, new { connectionId })
                );
            }

            return new APIGatewayProxyResponse { StatusCode = 200 };
        }
        catch (Exception ex)
        {
            context.Logger.LogError($"Error handling connect: {ex.Message}");
            return new APIGatewayProxyResponse { StatusCode = 500 };
        }
    }

    private async Task<APIGatewayProxyResponse> HandleDisconnectAsync(
        APIGatewayProxyRequest request,
        ILambdaContext context
    )
    {
        var connectionId = request.RequestContext.ConnectionId;
        context.Logger.LogInformation($"Disconnection: {connectionId}");

        try
        {
            // Get connection info
            var connection = await _services.ConnectionRepository.GetByConnectionIdAsync(
                connectionId
            );

            if (connection == null)
            {
                context.Logger.LogWarning($"No connection record found for {connectionId}");
                return new APIGatewayProxyResponse { StatusCode = 200 };
            }

            // Check if player is in an active game
            if (!string.IsNullOrEmpty(connection.GameCode))
            {
                await HandleGameDisconnectionAsync(connection, context);
            }

            // Delete connection record
            await _services.ConnectionRepository.DeleteAsync(connectionId);
            context.Logger.LogInformation($"Deleted connection record for {connectionId}");

            return new APIGatewayProxyResponse { StatusCode = 200 };
        }
        catch (Exception ex)
        {
            context.Logger.LogError($"Error handling disconnect: {ex.Message}");
            return new APIGatewayProxyResponse { StatusCode = 200 }; // Return 200 even on error
        }
    }

    private async Task HandleGameDisconnectionAsync(
        ConnectionRecord connection,
        ILambdaContext context
    )
    {
        if (connection.GameCode == null)
            return;

        var game = await _services.GameRepository.GetByGameCodeAsync(connection.GameCode);
        if (game == null || game.HasEnded)
        {
            return;
        }

        context.Logger.LogInformation($"Player disconnected from active game {game.GameCode}");

        // Determine which player disconnected
        var isHost = game.HostConnectionId == connection.ConnectionId;
        var playerRole = isHost ? "HOST" : "GUEST";

        // Mark player as disconnected
        if (isHost)
        {
            game.HostDisconnectedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }
        else
        {
            game.GuestDisconnectedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        // Update game
        await _services.GameRepository.UpdateAsync(game);

        // Start disconnect timer (30 second grace period)
        var disconnectTimer = GameTimerRecord.CreateDisconnectTimer(
            game.GameCode,
            playerRole,
            gracePeriodSeconds: 30
        );
        await _services.TimerRepository.CreateAsync(disconnectTimer);

        context.Logger.LogInformation(
            $"Created disconnect timer for {playerRole} in game {game.GameCode}"
        );

        // Notify opponent
        var opponentConnectionId = isHost ? game.GuestConnectionId : game.HostConnectionId;
        await _services.WebSocketService.SendMessageAsync(
            opponentConnectionId,
            new WebSocketMessage(MessageTypes.OpponentDisconnected, new { playerRole })
        );
    }

    private Task<ActiveGameRecord?> TryReconnectPlayerAsync(
        string connectionId,
        ILambdaContext context
    )
    {
        // This is a simplified reconnection - in a real implementation, you might want to:
        // 1. Pass player identification (e.g., user ID) via query params
        // 2. Look for games where this player is disconnected
        // 3. Update the connection ID

        // For now, we'll return null (no reconnection support in this initial implementation)
        // This will be implemented when we add authentication

        return Task.FromResult<ActiveGameRecord?>(null);
    }
}
