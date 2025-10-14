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

public class AcceptDrawOfferCommandHandler(
    IActiveGameRepository activeGameRepository,
    WebSocketService webSocketService,
    ILogger<AcceptDrawOfferCommandHandler> logger
) : INotificationHandler<AcceptDrawOfferCommand>
{
    private readonly IActiveGameRepository _activeGameRepository = activeGameRepository;
    private readonly WebSocketService _webSocketService = webSocketService;
    private readonly ILogger<AcceptDrawOfferCommandHandler> _logger = logger;

    public async Task Handle(AcceptDrawOfferCommand command, CancellationToken cancellationToken)
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

            // Accept draw offer
            var results = game.AcceptDrawOffer(command.ConnectionId);
            if (results.Contains(AcceptDrawOfferResults.NoOfferToAccept))
            {
                _logger.LogWarning(
                    $"No draw offer to accept for connection {command.ConnectionId} in game {activeGameRecord.GameCode}"
                );
                return;
            }

            // Remove game from repository
            await _activeGameRepository.DeleteAsync(activeGameRecord.GameCode);

            // TODO: Remove timers when timer service is implemented

            // Send game over messages to both players
            var gameOverReason = GameOverReason.DrawByAgreement;
            await SendGameOverMessagesAsync(
                activeGameRecord,
                game,
                gameOverReason,
                0,
                0 // TODO: Get actual elapsed seconds from timer service
            );

            _logger.LogInformation(
                $"Game {activeGameRecord.GameCode} ended by draw agreement accepted by connection {command.ConnectionId}"
            );
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error handling acceptDrawOffer command: {ex.Message}");
        }
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
