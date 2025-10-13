using System.Text.Json;
using ChessOfCards.Domain.Features.Games;
using ChessOfCards.GameActionHandler.Application.Features.Games.Commands;
using ChessOfCards.Infrastructure.Messages;
using ChessOfCards.Infrastructure.Repositories;
using ChessOfCards.Infrastructure.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChessOfCards.GameActionHandler.Application.Features.Games.Handlers;

public class SendChatMessageCommandHandler(
    IActiveGameRepository activeGameRepository,
    WebSocketService webSocketService,
    ILogger<SendChatMessageCommandHandler> logger
) : INotificationHandler<SendChatMessageCommand>
{
    private readonly IActiveGameRepository _activeGameRepository = activeGameRepository;
    private readonly WebSocketService _webSocketService = webSocketService;
    private readonly ILogger<SendChatMessageCommandHandler> _logger = logger;

    public async Task Handle(SendChatMessageCommand command, CancellationToken cancellationToken)
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

            // Send chat message
            var results = game.SendChatMessage(command.ConnectionId, command.RawMessage);
            if (results.Contains(SendChatMessageResults.MessageHasNoContent))
            {
                _logger.LogWarning(
                    $"Empty chat message from connection {command.ConnectionId} in game {activeGameRecord.GameCode}"
                );
                return;
            }

            // TODO: Get elapsed time from timer service when implemented
            var hostSecondsElapsed = 0.0;
            var guestSecondsElapsed = 0.0;

            // Update game state in repository
            activeGameRecord.GameState = JsonSerializer.Serialize(game);
            await _activeGameRepository.UpdateAsync(activeGameRecord);

            // Notify both players of chat message
            await SendChatMessageSentAsync(
                activeGameRecord,
                game,
                hostSecondsElapsed,
                guestSecondsElapsed
            );

            _logger.LogInformation(
                $"Chat message sent by connection {command.ConnectionId} in game {activeGameRecord.GameCode}"
            );
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error handling sendChatMessage command: {ex.Message}");
        }
    }

    private async Task SendChatMessageSentAsync(
        Infrastructure.Models.ActiveGameRecord activeGameRecord,
        Game game,
        double hostSecondsElapsed,
        double guestSecondsElapsed
    )
    {
        var hostView = game.ToHostPlayerView(hostSecondsElapsed, guestSecondsElapsed);
        var guestView = game.ToGuestPlayerView(hostSecondsElapsed, guestSecondsElapsed);

        var hostMessage = new WebSocketMessage(MessageTypes.ChatMessageSent, hostView);
        var guestMessage = new WebSocketMessage(MessageTypes.ChatMessageSent, guestView);

        await _webSocketService.SendMessageAsync(activeGameRecord.HostConnectionId, hostMessage);
        await _webSocketService.SendMessageAsync(activeGameRecord.GuestConnectionId, guestMessage);
    }
}
