using ChessOfCards.GameActionHandler.Application.Features.Games.Commands;
using ChessOfCards.Infrastructure.Messages;
using ChessOfCards.Infrastructure.Models;
using ChessOfCards.Infrastructure.Repositories;
using ChessOfCards.Infrastructure.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChessOfCards.GameActionHandler.Application.Features.Games.Handlers;

public class CreatePendingGameCommandHandler(
    IPendingGameRepository pendingGameRepository,
    IConnectionRepository connectionRepository,
    WebSocketService webSocketService,
    ILogger<CreatePendingGameCommandHandler> logger
) : IRequestHandler<CreatePendingGameCommand>
{
    private readonly IPendingGameRepository _pendingGameRepository = pendingGameRepository;
    private readonly IConnectionRepository _connectionRepository = connectionRepository;
    private readonly WebSocketService _webSocketService = webSocketService;
    private readonly ILogger<CreatePendingGameCommandHandler> _logger = logger;

    public async Task Handle(CreatePendingGameCommand command, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation($"Creating pending game for {command.ConnectionId}");

            // Validate game name
            if (!string.IsNullOrWhiteSpace(command.HostName) && ContainsBadWords(command.HostName))
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

            // Generate game code (4 character alphanumeric)
            var gameCode = GenerateGameCode();

            // Create pending game record
            var pendingGame = new PendingGameRecord(
                gameCode,
                command.ConnectionId,
                command.DurationOption ?? "MEDIUM",
                command.HostName
            );

            await _pendingGameRepository.CreateAsync(pendingGame);

            // Update connection record
            var connection = await _connectionRepository.GetByConnectionIdAsync(
                command.ConnectionId
            );
            if (connection != null)
            {
                connection.GameCode = gameCode;
                connection.PlayerRole = "HOST";
                connection.PlayerName = command.HostName;
                await _connectionRepository.UpdateAsync(connection);
            }

            _logger.LogInformation($"Created pending game {gameCode}");

            // Send response to client
            await _webSocketService.SendMessageAsync(
                command.ConnectionId,
                new WebSocketMessage(
                    MessageTypes.CreatedPendingGame,
                    new
                    {
                        gameCode,
                        durationOption = pendingGame.DurationOption,
                        hostName = pendingGame.HostName,
                    }
                )
            );
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error creating pending game: {ex.Message}");
            await SendErrorAsync(command.ConnectionId, "Failed to create game");
        }
    }

    private static string GenerateGameCode()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // Exclude similar looking characters
        var random = new Random();
        return new string(
            Enumerable.Repeat(chars, 4).Select(s => s[random.Next(s.Length)]).ToArray()
        );
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
