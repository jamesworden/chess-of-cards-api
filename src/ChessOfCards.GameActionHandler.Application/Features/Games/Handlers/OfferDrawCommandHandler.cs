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

public class OfferDrawCommandHandler(
    IActiveGameRepository activeGameRepository,
    WebSocketService webSocketService,
    ILogger<OfferDrawCommandHandler> logger
) : INotificationHandler<OfferDrawCommand>
{
    private readonly IActiveGameRepository _activeGameRepository = activeGameRepository;
    private readonly WebSocketService _webSocketService = webSocketService;
    private readonly ILogger<OfferDrawCommandHandler> _logger = logger;

    public async Task Handle(OfferDrawCommand command, CancellationToken cancellationToken)
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

            // Offer draw
            var results = game.OfferDraw(command.ConnectionId);
            if (results.Contains(OfferDrawResults.AlreadyOfferedDraw))
            {
                _logger.LogInformation(
                    $"Connection {command.ConnectionId} already offered draw in game {activeGameRecord.GameCode}"
                );
                return;
            }

            // Update game state in repository
            activeGameRecord.GameState = JsonSerializer.Serialize(game, JsonOptions.Default);
            await _activeGameRepository.UpdateAsync(activeGameRecord);

            // Determine opponent and send draw offer message
            var opponentConnectionId =
                activeGameRecord.HostConnectionId == command.ConnectionId
                    ? activeGameRecord.GuestConnectionId
                    : activeGameRecord.HostConnectionId;

            var drawOfferedMessage = new WebSocketMessage(MessageTypes.DrawOffered, null);
            await _webSocketService.SendMessageAsync(opponentConnectionId, drawOfferedMessage);

            _logger.LogInformation(
                $"Draw offered by connection {command.ConnectionId} to {opponentConnectionId} in game {activeGameRecord.GameCode}"
            );
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error handling offerDraw command: {ex.Message}");
        }
    }
}
