using ChessOfCards.DataAccess.Interfaces;
using ChessOfCards.Domain.Features.Games;
using MediatR;

namespace ChessOfCards.Application.Features.Games;

public class PassMoveCommandHandler(
  IGameRepository gameRepository,
  IMediator mediator,
  IGameTimerService gameTimerService
) : IRequestHandler<PassMoveCommand>
{
  private readonly IGameRepository _gameRepository = gameRepository;

  private readonly IMediator _mediator = mediator;

  private readonly IGameTimerService _gameTimerService = gameTimerService;

  public async Task Handle(PassMoveCommand command, CancellationToken cancellationToken)
  {
    var game = _gameRepository.FindByConnectionId(command.ConnectionId);
    if (game is null)
    {
      return;
    }

    var passMoveResults = game.PassMove(command.ConnectionId);
    if (passMoveResults.Contains(PassMoveResults.NotPlayersTurn))
    {
      return;
    }

    if (game.HasEnded)
    {
      _gameRepository.RemoveByGameCode(game.GameCode);

      var (finalHostSeconds, finalGuestSeconds) = _gameTimerService.RemoveTimers(game.GameCode);

      await _mediator.Publish(
        new GameOverCommand(
          game,
          GameOverReason.DrawByRepetition,
          finalHostSeconds,
          finalGuestSeconds
        ),
        cancellationToken
      );
      return;
    }

    var (hostSecondsElapsed, guestSecondsElapsed) = game.IsHostPlayersTurn
      ? _gameTimerService.StartHostTimer(game.GameCode)
      : _gameTimerService.StartGuestTimer(game.GameCode);

    await _mediator.Publish(
      new GameUpdatedCommand(game, hostSecondsElapsed, guestSecondsElapsed),
      cancellationToken
    );
  }
}
