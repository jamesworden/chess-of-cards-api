using ChessOfCards.GameActionHandler.Application.Features.Games.Commands;
using ChessOfCards.Infrastructure.Messages;
using ChessOfCards.Infrastructure.Models;
using ChessOfCards.Infrastructure.Repositories;
using ChessOfCards.Infrastructure.Services;
using ChessOfCards.Shared.Utilities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChessOfCards.GameActionHandler.Application.Features.Games.Handlers;

public class JoinGameCommandHandler(
    IPendingGameRepository pendingGameRepository,
    IActiveGameRepository activeGameRepository,
    IConnectionRepository connectionRepository,
    WebSocketService webSocketService,
    ILogger<JoinGameCommandHandler> logger
) : IRequestHandler<JoinGameCommand>
{
    private readonly IPendingGameRepository _pendingGameRepository = pendingGameRepository;
    private readonly IActiveGameRepository _activeGameRepository = activeGameRepository;
    private readonly IConnectionRepository _connectionRepository = connectionRepository;
    private readonly WebSocketService _webSocketService = webSocketService;
    private readonly ILogger<JoinGameCommandHandler> _logger = logger;

    public async Task Handle(JoinGameCommand command, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation($"Player joining game {command.GameCode}");

            // Validate guest name
            if (
                !string.IsNullOrWhiteSpace(command.GuestName) && ContainsBadWords(command.GuestName)
            )
            {
                await _webSocketService.SendMessageAsync(
                    command.ConnectionId,
                    new WebSocketMessage(
                        MessageTypes.GameNameInvalid,
                        new { reason = "Name contains inappropriate content" }
                    )
                );
                return;
            }

            // Find pending game
            var pendingGame = await _pendingGameRepository.GetByGameCodeAsync(
                command.GameCode.ToUpper()
            );
            if (pendingGame == null)
            {
                await _webSocketService.SendMessageAsync(
                    command.ConnectionId,
                    new WebSocketMessage(
                        MessageTypes.JoinGameCodeInvalid,
                        new { gameCode = command.GameCode }
                    )
                );
                return;
            }

            // Can't join your own game
            if (pendingGame.HostConnectionId == command.ConnectionId)
            {
                await SendErrorAsync(command.ConnectionId, "Cannot join your own game");
                return;
            }

            _logger.LogInformation($"Starting game {pendingGame.GameCode}");

            // Parse duration option from string to enum
            var durationOption = Enum.Parse<Domain.Features.Games.DurationOption>(
                pendingGame.DurationOption,
                ignoreCase: true
            );

            // Create new Game domain object
            var game = new Domain.Features.Games.Game(
                pendingGame.HostConnectionId,
                command.ConnectionId,
                pendingGame.GameCode,
                durationOption,
                pendingGame.HostName,
                command.GuestName
            );

            // Serialize game state to JSON
            var gameStateJson = System.Text.Json.JsonSerializer.Serialize(
                game,
                JsonOptions.Default
            );

            // Create active game record
            var activeGame = new ActiveGameRecord(
                pendingGame.GameCode,
                pendingGame.HostConnectionId,
                command.ConnectionId,
                pendingGame.DurationOption,
                pendingGame.HostName,
                command.GuestName,
                gameStateJson
            );

            await _activeGameRepository.CreateAsync(activeGame);

            // Delete pending game
            await _pendingGameRepository.DeleteAsync(pendingGame.GameCode);

            // Update guest connection
            var guestConnection = await _connectionRepository.GetByConnectionIdAsync(
                command.ConnectionId
            );
            if (guestConnection != null)
            {
                guestConnection.GameCode = pendingGame.GameCode;
                guestConnection.PlayerRole = "GUEST";
                guestConnection.PlayerName = command.GuestName;
                await _connectionRepository.UpdateAsync(guestConnection);
            }

            _logger.LogInformation($"Game {pendingGame.GameCode} started");

            // TODO: Start game timer when timer service is implemented
            var hostSecondsElapsed = 0.0;
            var guestSecondsElapsed = 0.0;

            // Notify both players with full game state views
            var hostView = game.ToHostPlayerView(hostSecondsElapsed, guestSecondsElapsed);
            var guestView = game.ToGuestPlayerView(hostSecondsElapsed, guestSecondsElapsed);

            var hostGameStartedMessage = new WebSocketMessage(
                MessageTypes.GameStarted,
                new { gameView = hostView }
            );

            var guestGameStartedMessage = new WebSocketMessage(
                MessageTypes.GameStarted,
                new { gameView = guestView }
            );

            await _webSocketService.SendMessageAsync(
                pendingGame.HostConnectionId,
                hostGameStartedMessage
            );
            await _webSocketService.SendMessageAsync(command.ConnectionId, guestGameStartedMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error joining game: {ex.Message}");
            await SendErrorAsync(command.ConnectionId, "Failed to join game");
        }
    }

    private static bool ContainsBadWords(string text)
    {
        // Simplified bad word check - in production, use a proper profanity filter
        var badWords = new[] { "badword1", "badword2" }; // Placeholder
        return badWords.Any(word => text.Contains(word, StringComparison.OrdinalIgnoreCase));
    }

    private async Task SendErrorAsync(string connectionId, string message)
    {
        await _webSocketService.SendMessageAsync(
            connectionId,
            new WebSocketMessage(MessageTypes.Error, new { error = message })
        );
    }
}
