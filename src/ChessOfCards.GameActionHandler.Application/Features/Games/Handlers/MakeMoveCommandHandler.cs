using System.Text.Json;
using ChessOfCards.Domain.Features.Games;
using ChessOfCards.GameActionHandler.Application.Features.Games.Commands;
using ChessOfCards.Infrastructure.Messages;
using ChessOfCards.Infrastructure.Repositories;
using ChessOfCards.Infrastructure.Services;
using ChessOfCards.Shared.Utilities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChessOfCards.GameActionHandler.Application.Features.Games.Handlers;

public class MakeMoveCommandHandler(
    IActiveGameRepository activeGameRepository,
    WebSocketService webSocketService,
    ILogger<MakeMoveCommandHandler> logger
) : INotificationHandler<MakeMoveCommand>
{
    private readonly IActiveGameRepository _activeGameRepository = activeGameRepository;
    private readonly WebSocketService _webSocketService = webSocketService;
    private readonly ILogger<MakeMoveCommandHandler> _logger = logger;

    public async Task Handle(MakeMoveCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var activeGameRecord = await _activeGameRepository.GetByConnectionIdAsync(
                command.ConnectionId
            );
            if (activeGameRecord == null)
            {
                _logger.LogWarning($"No active game found for connection {command.ConnectionId}");
                return;
            }

            // Deserialize game state
            var game = JsonSerializer.Deserialize<Game>(
                activeGameRecord.GameState,
                JsonOptions.Default
            );
            if (game == null)
            {
                _logger.LogError(
                    $"Failed to deserialize game state for {activeGameRecord.GameCode}"
                );
                return;
            }

            // Execute move
            var results = game.MakeMove(
                command.ConnectionId,
                command.Move,
                command.RearrangedCardsInHand
            );

            // Handle invalid move results
            if (results.Contains(MakeMoveResults.InvalidCards))
            {
                _logger.LogWarning(
                    $"Invalid cards for connection {command.ConnectionId} in game {activeGameRecord.GameCode}"
                );
                return;
            }
            else if (results.Contains(MakeMoveResults.InvalidMove))
            {
                _logger.LogWarning(
                    $"Invalid move for connection {command.ConnectionId} in game {activeGameRecord.GameCode}"
                );
                return;
            }

            // Handle game over
            if (game.HasEnded)
            {
                await _activeGameRepository.DeleteAsync(activeGameRecord.GameCode);

                // TODO: Remove timers when timer service is implemented

                var gameOverReason = GameOverReason.DrawByRepetition; // Based on three passes or no moves
                await SendGameOverMessagesAsync(
                    activeGameRecord,
                    game,
                    gameOverReason,
                    0,
                    0 // TODO: Get actual elapsed seconds from timer service
                );
                return;
            }

            // TODO: Start appropriate timer when timer service is implemented
            // For now, use placeholder values
            var hostSecondsElapsed = 0.0;
            var guestSecondsElapsed = 0.0;

            // Handle turn skipped scenarios
            if (results.Contains(MakeMoveResults.HostTurnSkippedNoMoves))
            {
                await SendTurnSkippedMessageAsync(
                    activeGameRecord.HostConnectionId,
                    game,
                    true,
                    hostSecondsElapsed,
                    guestSecondsElapsed
                );
            }
            else if (results.Contains(MakeMoveResults.GuestTurnSkippedNoMoves))
            {
                await SendTurnSkippedMessageAsync(
                    activeGameRecord.GuestConnectionId,
                    game,
                    false,
                    hostSecondsElapsed,
                    guestSecondsElapsed
                );
            }

            // Update game state in repository
            activeGameRecord.GameState = JsonSerializer.Serialize(game, JsonOptions.Default);
            activeGameRecord.IsHostPlayersTurn = game.IsHostPlayersTurn;
            activeGameRecord.HasEnded = game.HasEnded;
            activeGameRecord.WonBy = game.WonBy.ToString().ToUpper();
            await _activeGameRepository.UpdateAsync(activeGameRecord);

            // Notify both players of game update
            await SendGameUpdatedMessagesAsync(
                activeGameRecord,
                game,
                hostSecondsElapsed,
                guestSecondsElapsed
            );
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error handling makeMove command: {ex.Message}");
        }
    }

    private async Task SendGameUpdatedMessagesAsync(
        Infrastructure.Models.ActiveGameRecord activeGameRecord,
        Game game,
        double hostSecondsElapsed,
        double guestSecondsElapsed
    )
    {
        var hostView = game.ToHostPlayerView(hostSecondsElapsed, guestSecondsElapsed);
        var guestView = game.ToGuestPlayerView(hostSecondsElapsed, guestSecondsElapsed);

        var hostMessage = new WebSocketMessage(MessageTypes.GameUpdated, hostView);
        var guestMessage = new WebSocketMessage(MessageTypes.GameUpdated, guestView);

        await _webSocketService.SendMessageAsync(activeGameRecord.HostConnectionId, hostMessage);
        await _webSocketService.SendMessageAsync(activeGameRecord.GuestConnectionId, guestMessage);
    }

    private async Task SendTurnSkippedMessageAsync(
        string connectionId,
        Game game,
        bool isHost,
        double hostSecondsElapsed,
        double guestSecondsElapsed
    )
    {
        var playerView = isHost
            ? game.ToHostPlayerView(hostSecondsElapsed, guestSecondsElapsed)
            : game.ToGuestPlayerView(hostSecondsElapsed, guestSecondsElapsed);

        var message = new WebSocketMessage(MessageTypes.TurnSkipped, playerView);
        await _webSocketService.SendMessageAsync(connectionId, message);
    }

    private async Task SendGameOverMessagesAsync(
        Infrastructure.Models.ActiveGameRecord activeGameRecord,
        Game game,
        GameOverReason reason,
        double hostSecondsElapsed,
        double guestSecondsElapsed
    )
    {
        var hostView = game.ToHostPlayerView(hostSecondsElapsed, guestSecondsElapsed);
        var guestView = game.ToGuestPlayerView(hostSecondsElapsed, guestSecondsElapsed);

        var hostMessage = new WebSocketMessage(
            MessageTypes.GameOver,
            new { gameView = hostView, reason = reason.ToString() }
        );
        var guestMessage = new WebSocketMessage(
            MessageTypes.GameOver,
            new { gameView = guestView, reason = reason.ToString() }
        );

        await _webSocketService.SendMessageAsync(activeGameRecord.HostConnectionId, hostMessage);
        await _webSocketService.SendMessageAsync(activeGameRecord.GuestConnectionId, guestMessage);
    }
}
