using ChessOfCards.DataAccess.Interfaces;
using ChessOfCards.Domain.Features.Games;
using MediatR;

namespace ChessOfCards.Application.Features.Games;

public class MakeMoveCommandHandler(
  IGameRepository gameRepository,
  IMediator mediator,
  IGameTimerService gameTimerService
) : INotificationHandler<MakeMoveCommand>
{
  private readonly IGameRepository _gameRepository = gameRepository;

  private readonly IMediator _mediator = mediator;

  private readonly IGameTimerService _gameTimerService = gameTimerService;

  public async Task Handle(MakeMoveCommand command, CancellationToken cancellationToken)
  {
    var game = _gameRepository.FindByConnectionId(command.ConnectionId);
    if (game is null)
    {
      return;
    }
    var results = game.MakeMove(command.ConnectionId, command.Move, command.RearrangedCardsInHand);
    if (results.Contains(MakeMoveResults.InvalidCards))
    {
      return;
    }
    else if (results.Contains(MakeMoveResults.InvalidMove))
    {
      return;
    }
    else if (game.HasEnded)
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

    if (results.Contains(MakeMoveResults.HostTurnSkippedNoMoves))
    {
      await _mediator.Publish(
        new TurnSkippedCommand(
          game.HostConnectionId,
          game.ToHostPlayerView(hostSecondsElapsed, guestSecondsElapsed)
        ),
        cancellationToken
      );
    }
    else if (results.Contains(MakeMoveResults.GuestTurnSkippedNoMoves))
    {
      await _mediator.Publish(
        new TurnSkippedCommand(
          game.GuestConnectionId,
          game.ToGuestPlayerView(hostSecondsElapsed, guestSecondsElapsed)
        ),
        cancellationToken
      );
    }

    await _mediator.Publish(
      new GameUpdatedCommand(game, hostSecondsElapsed, guestSecondsElapsed),
      cancellationToken
    );
  }
}
