using ChessOfCards.DataAccess.Interfaces;
using ChessOfCards.Domain.Features.Games;
using MediatR;

namespace ChessOfCards.Application.Features.Games;

public class DisconnectTimerExpiredCommandHandler(
  IGameRepository gameRepository,
  IMediator mediator,
  IGameTimerService gameTimerService
) : INotificationHandler<DisconnectTimerExpiredCommand>
{
  private readonly IGameRepository _gameRepository = gameRepository;

  private readonly IMediator _mediator = mediator;

  private readonly IGameTimerService _gameTimerService = gameTimerService;

  public async Task Handle(
    DisconnectTimerExpiredCommand command,
    CancellationToken cancellationToken
  )
  {
    var game = _gameRepository.FindByGameCode(command.GameCode);
    if (game is null)
    {
      return;
    }

    game.EndByDisconnection();

    _gameRepository.RemoveByGameCode(game.GameCode);

    var (hostSecondsElapsed, guestSecondsElapsed) = _gameTimerService.RemoveTimers(game.GameCode);

    await _mediator.Publish(
      new GameOverCommand(
        game,
        GameOverReason.Disconnected,
        hostSecondsElapsed,
        guestSecondsElapsed
      ),
      cancellationToken
    );
  }
}
