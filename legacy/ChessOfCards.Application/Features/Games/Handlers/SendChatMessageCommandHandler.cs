using ChessOfCards.DataAccess.Interfaces;
using ChessOfCards.Domain.Features.Games;
using MediatR;

namespace ChessOfCards.Application.Features.Games;

public class SendChatMessageCommandHandler(
  IGameRepository gameRepository,
  IMediator mediator,
  IGameTimerService gameTimerService
) : INotificationHandler<SendChatMessageCommand>
{
  private readonly IGameRepository _gameRepository = gameRepository;

  private readonly IMediator _mediator = mediator;

  private readonly IGameTimerService _gameTimerService = gameTimerService;

  public async Task Handle(SendChatMessageCommand command, CancellationToken cancellationToken)
  {
    var game = _gameRepository.FindByConnectionId(command.ConnectionId);
    if (game is null)
    {
      return;
    }

    var results = game.SendChatMessage(command.ConnectionId, command.RawMessage);
    if (results.Contains(SendChatMessageResults.MessageHasNoContent))
    {
      return;
    }

    var (hostSecondsElapsed, guestSecondsElapsed) = _gameTimerService.GetElapsedTime(game.GameCode);

    await _mediator.Publish(
      new ChatMessageSentCommand(game, hostSecondsElapsed, guestSecondsElapsed),
      cancellationToken
    );
  }
}
