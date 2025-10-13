using System.Text.Json;
using ChessOfCards.Domain.Features.Games;
using ChessOfCards.GameActionHandler.Application.Features.Games.Commands;
using ChessOfCards.Infrastructure.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChessOfCards.GameActionHandler.Application.Features.Games.Handlers;

public class RearrangeHandCommandHandler(
    IActiveGameRepository activeGameRepository,
    ILogger<RearrangeHandCommandHandler> logger
) : INotificationHandler<RearrangeHandCommand>
{
    private readonly IActiveGameRepository _activeGameRepository = activeGameRepository;
    private readonly ILogger<RearrangeHandCommandHandler> _logger = logger;

    public async Task Handle(RearrangeHandCommand command, CancellationToken cancellationToken)
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
            var game = JsonSerializer.Deserialize<Game>(activeGameRecord.GameState);
            if (game == null)
            {
                _logger.LogError(
                    $"Failed to deserialize game state for {activeGameRecord.GameCode}"
                );
                return;
            }

            // Rearrange hand (silent operation - no validation needed for invalid cards as it's handled internally)
            game.RearrangeHand(command.ConnectionId, command.Cards);

            // Update game state in repository
            activeGameRecord.GameState = JsonSerializer.Serialize(game);
            await _activeGameRepository.UpdateAsync(activeGameRecord);

            // No need to notify opponent - this is a silent client-side operation
            _logger.LogInformation(
                $"Hand rearranged for connection {command.ConnectionId} in game {activeGameRecord.GameCode}"
            );
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error handling rearrangeHand command: {ex.Message}");
        }
    }
}
