using System.Text.Json;
using ChessOfCards.Domain.Features.Games;
using ChessOfCards.GameActionHandler.Application.Features.Games.Commands;
using ChessOfCards.Infrastructure.Repositories;
using ChessOfCards.Shared.Utilities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChessOfCards.GameActionHandler.Application.Features.Games.Handlers;

public class MarkLatestReadChatMessageCommandHandler(
    IActiveGameRepository activeGameRepository,
    ILogger<MarkLatestReadChatMessageCommandHandler> logger
) : INotificationHandler<MarkLatestReadChatMessageCommand>
{
    private readonly IActiveGameRepository _activeGameRepository = activeGameRepository;
    private readonly ILogger<MarkLatestReadChatMessageCommandHandler> _logger = logger;

    public async Task Handle(
        MarkLatestReadChatMessageCommand command,
        CancellationToken cancellationToken
    )
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

            // Mark latest read chat message index
            game.MarkLatestReadChatMessageIndex(command.ConnectionId, command.LatestIndex);

            // Update game state in repository
            activeGameRecord.GameState = JsonSerializer.Serialize(game, JsonOptions.Default);
            await _activeGameRepository.UpdateAsync(activeGameRecord);

            // No notification needed - this is a silent operation to track read status
            _logger.LogInformation(
                $"Marked chat message index {command.LatestIndex} as read for connection {command.ConnectionId} in game {activeGameRecord.GameCode}"
            );
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error handling markLatestReadChatMessage command: {ex.Message}");
        }
    }
}
