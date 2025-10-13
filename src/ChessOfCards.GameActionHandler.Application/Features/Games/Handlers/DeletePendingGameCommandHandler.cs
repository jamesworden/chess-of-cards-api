using ChessOfCards.GameActionHandler.Application.Features.Games.Commands;
using ChessOfCards.Infrastructure.Messages;
using ChessOfCards.Infrastructure.Repositories;
using ChessOfCards.Infrastructure.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChessOfCards.GameActionHandler.Application.Features.Games.Handlers;

public class DeletePendingGameCommandHandler(
    IPendingGameRepository pendingGameRepository,
    IConnectionRepository connectionRepository,
    WebSocketService webSocketService,
    ILogger<DeletePendingGameCommandHandler> logger
) : IRequestHandler<DeletePendingGameCommand>
{
    private readonly IPendingGameRepository _pendingGameRepository = pendingGameRepository;
    private readonly IConnectionRepository _connectionRepository = connectionRepository;
    private readonly WebSocketService _webSocketService = webSocketService;
    private readonly ILogger<DeletePendingGameCommandHandler> _logger = logger;

    public async Task Handle(DeletePendingGameCommand command, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation($"Deleting pending game for {command.ConnectionId}");

            // Find pending game by host connection
            var pendingGame = await _pendingGameRepository.GetByHostConnectionIdAsync(
                command.ConnectionId
            );
            if (pendingGame == null)
            {
                await SendErrorAsync(command.ConnectionId, "No pending game found");
                return;
            }

            // Delete pending game
            await _pendingGameRepository.DeleteAsync(pendingGame.GameCode);

            // Update connection record
            var connection = await _connectionRepository.GetByConnectionIdAsync(
                command.ConnectionId
            );
            if (connection != null)
            {
                connection.GameCode = null;
                connection.PlayerRole = null;
                await _connectionRepository.UpdateAsync(connection);
            }

            _logger.LogInformation($"Deleted pending game {pendingGame.GameCode}");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error deleting pending game: {ex.Message}");
            await SendErrorAsync(command.ConnectionId, "Failed to delete game");
        }
    }

    private async Task SendErrorAsync(string connectionId, string message)
    {
        await _webSocketService.SendMessageAsync(
            connectionId,
            new WebSocketMessage(MessageTypes.Error, new { error = message })
        );
    }
}
