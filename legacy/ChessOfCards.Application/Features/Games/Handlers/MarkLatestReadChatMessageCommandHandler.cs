using ChessOfCards.DataAccess.Interfaces;
using ChessOfCards.Domain.Features.Games;
using MediatR;

namespace ChessOfCards.Application.Features.Games;

public class MarkLatestReadChatMessageCommandHandler(
  IGameRepository gameRepository,
  IMediator mediator,
  IGameTimerService gameTimerService
) : INotificationHandler<MarkLatestReadChatMessageCommand>
{
  private readonly IGameRepository _gameRepository = gameRepository;

  private readonly IMediator _mediator = mediator;

  private readonly IGameTimerService _gameTimerService = gameTimerService;

  public async Task Handle(
    MarkLatestReadChatMessageCommand command,
    CancellationToken cancellationToken
  )
  {
    var game = _gameRepository.FindByConnectionId(command.ConnectionId);
    if (game is null)
    {
      return;
    }

    game.MarkLatestReadChatMessageIndex(command.ConnectionId, command.LatestIndex);

    var (hostSecondsElapsed, guestSecondsElapsed) = _gameTimerService.GetElapsedTime(game.GameCode);

    await _mediator.Publish(
      new LatestReadChatMessageMarkedCommand(game, hostSecondsElapsed, guestSecondsElapsed),
      cancellationToken
    );

    await Task.CompletedTask;
  }
}
