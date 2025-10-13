using ChessOfCards.DataAccess.Interfaces;
using ChessOfCards.Domain.Features.Games;
using ChessOfCards.Domain.Shared.Util;
using MediatR;

namespace ChessOfCards.Application.Features.Games;

public class JoinGameCommandHandler(
  IPendingGameRepository pendingGameRepository,
  IGameRepository gameRepository,
  IMediator mediator,
  IGameTimerService gameTimerService
) : IRequestHandler<JoinGameCommand>
{
  private readonly IPendingGameRepository _pendingGameRepository = pendingGameRepository;

  private readonly IGameRepository _gameRepository = gameRepository;

  private readonly IMediator _mediator = mediator;

  private readonly IGameTimerService _gameTimerService = gameTimerService;

  public async Task Handle(JoinGameCommand command, CancellationToken cancellationToken)
  {
    var name = string.IsNullOrWhiteSpace(command.Name) ? null : command.Name;
    if (name is not null && (name.ReplaceBadWordsWithAsterisks() != name))
    {
      await _mediator.Publish(new GameNameInvalidCommand(command.ConnectionId), cancellationToken);
      return;
    }

    var (existingGame, reconnectedAsHost) = ReconnectToExistingGame(
      command.ConnectionId,
      command.GameCode,
      name
    );
    if (existingGame is not null)
    {
      var (hostSecondsElapsed, guestSecondsElapsed) = _gameTimerService.GetElapsedTime(
        existingGame.GameCode
      );

      var playerGameView = reconnectedAsHost
        ? existingGame.ToHostPlayerView(hostSecondsElapsed, guestSecondsElapsed)
        : existingGame.ToGuestPlayerView(hostSecondsElapsed, guestSecondsElapsed);

      var opponentConnectionId = reconnectedAsHost
        ? existingGame.GuestConnectionId
        : existingGame.HostConnectionId;

      _gameTimerService.StopDisconnectTimer(existingGame.GameCode);

      await Task.WhenAll(
        [
          _mediator.Publish(
            new PlayerReconnectedCommand(command.ConnectionId, playerGameView),
            cancellationToken
          ),
          _mediator.Publish(
            new OpponentReconnectedCommand(opponentConnectionId),
            cancellationToken
          ),
        ]
      );
      return;
    }

    var game = JoinPendingGame(command.ConnectionId, command.GameCode, name);
    if (game is not null)
    {
      _gameTimerService.InitTimers(game.GameCode, game.DurationOption.ToSeconds());
      await _mediator.Publish(new GameStartedCommand(game), cancellationToken);
      return;
    }

    await _mediator.Publish(
      new JoinGameCodeInvalidCommand(command.ConnectionId),
      cancellationToken
    );
    return;
  }

  private (Game? game, bool reconnectedAsHost) ReconnectToExistingGame(
    string connectionId,
    string gameCode,
    string? guestName
  )
  {
    var game = _gameRepository.FindByGameCode(gameCode);
    if (game is null)
    {
      return (null, false);
    }

    var (reconnected, asHost) = game.ReconnectPlayer(connectionId, guestName);
    if (!reconnected)
    {
      return (null, false);
    }

    return (game, asHost);
  }

  private Game? JoinPendingGame(string connectionId, string gameCode, string? guestName)
  {
    var upperCaseGameCode = gameCode.ToUpper();
    var pendingGame = _pendingGameRepository.FindByGameCode(upperCaseGameCode);
    if (pendingGame is null)
    {
      return null;
    }

    _pendingGameRepository.RemoveByGameCode(upperCaseGameCode);

    var game = new Game(
      pendingGame.HostConnectionId,
      connectionId,
      upperCaseGameCode,
      pendingGame.DurationOption,
      pendingGame.HostName,
      guestName
    );

    _gameRepository.Add(game);

    return game;
  }
}
