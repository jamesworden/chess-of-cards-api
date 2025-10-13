using ChessOfCards.DataAccess.Interfaces;
using MediatR;

namespace ChessOfCards.Application.Features.Games;

public class PlayerDisconnectedCommandHandler(
  IGameRepository gameRepository,
  IPendingGameRepository pendingGameRepository,
  IMediator mediator,
  IGameTimerService gameTimerService
) : INotificationHandler<PlayerDisconnectedCommand>
{
  private readonly IGameRepository _gameRepository = gameRepository;

  private readonly IPendingGameRepository _pendingGameRepository = pendingGameRepository;

  private readonly IMediator _mediator = mediator;

  private readonly IGameTimerService _gameTimerService = gameTimerService;

  public async Task Handle(PlayerDisconnectedCommand command, CancellationToken cancellationToken)
  {
    var removedPendingGame = _pendingGameRepository.RemoveByConnectionId(command.ConnectionId);
    if (removedPendingGame)
    {
      return;
    }

    var game = _gameRepository.FindByConnectionId(command.ConnectionId);
    if (game is null)
    {
      return;
    }

    game.MarkPlayerAsDisconnected(command.ConnectionId);
    if (game.HasEnded)
    {
      _gameRepository.RemoveByGameCode(game.GameCode);
      return;
    }
    else
    {
      _gameTimerService.StartDisconnectTimer(game.GameCode);
    }

    var hostDisconnected = command.ConnectionId == game.HostConnectionId;
    var opponentConnectionId = hostDisconnected ? game.GuestConnectionId : game.HostConnectionId;
    await _mediator.Publish(
      new OpponentDisconnectedCommand(opponentConnectionId),
      cancellationToken
    );
  }
}
