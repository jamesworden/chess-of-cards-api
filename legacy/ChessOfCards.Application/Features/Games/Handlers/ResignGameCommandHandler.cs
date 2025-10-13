using ChessOfCards.DataAccess.Interfaces;
using ChessOfCards.Domain.Features.Games;
using MediatR;

namespace ChessOfCards.Application.Features.Games;

public class ResignGameCommandHandler(
  IGameRepository gameRepository,
  IMediator mediator,
  IGameTimerService gameTimerService
) : INotificationHandler<ResignGameCommand>
{
  private readonly IGameRepository _gameRepository = gameRepository;

  private readonly IMediator _mediator = mediator;

  private readonly IGameTimerService _gameTimerService = gameTimerService;

  public async Task Handle(ResignGameCommand command, CancellationToken cancellationToken)
  {
    var game = _gameRepository.FindByConnectionId(command.ConnectionId);
    if (game is null)
    {
      return;
    }

    game.Resign(command.ConnectionId);

    var (finalHostSeconds, finalGuestSeconds) = _gameTimerService.RemoveTimers(game.GameCode);

    await _mediator.Publish(
      new GameOverCommand(game, GameOverReason.Resigned, finalHostSeconds, finalGuestSeconds),
      cancellationToken
    );
  }
}
