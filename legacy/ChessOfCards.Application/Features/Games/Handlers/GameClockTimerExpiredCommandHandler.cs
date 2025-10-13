using ChessOfCards.DataAccess.Interfaces;
using ChessOfCards.Domain.Features.Games;
using MediatR;

namespace ChessOfCards.Application.Features.Games;

public class GameClockTimerExpiredCommandHandler(IGameRepository gameRepository, IMediator mediator)
  : INotificationHandler<GameClockTimerExpiredCommand>
{
  private readonly IGameRepository _gameRepository = gameRepository;

  private readonly IMediator _mediator = mediator;

  public async Task Handle(
    GameClockTimerExpiredCommand command,
    CancellationToken cancellationToken
  )
  {
    var game = _gameRepository.FindByGameCode(command.GameCode);
    if (game is null)
    {
      return;
    }

    game.EndByClockExpired();

    _gameRepository.RemoveByGameCode(game.GameCode);

    await _mediator.Publish(
      new GameOverCommand(
        game,
        GameOverReason.RanOutOfTime,
        command.HostSecondsElapsed,
        command.GuestSecondsElapsed
      ),
      cancellationToken
    );
  }
}
