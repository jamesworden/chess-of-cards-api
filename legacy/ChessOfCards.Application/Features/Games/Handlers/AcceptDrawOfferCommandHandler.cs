using ChessOfCards.DataAccess.Interfaces;
using ChessOfCards.Domain.Features.Games;
using MediatR;

namespace ChessOfCards.Application.Features.Games;

public class AcceptDrawOfferCommandHandler(
  IGameRepository gameRepository,
  IMediator mediator,
  IGameTimerService gameTimerService
) : INotificationHandler<AcceptDrawOfferCommand>
{
  private readonly IGameRepository _gameRepository = gameRepository;

  private readonly IMediator _mediator = mediator;

  private readonly IGameTimerService _gameTimerService = gameTimerService;

  public async Task Handle(AcceptDrawOfferCommand command, CancellationToken cancellationToken)
  {
    var game = _gameRepository.FindByConnectionId(command.ConnectionId);
    if (game is null)
    {
      return;
    }

    var results = game.AcceptDrawOffer(command.ConnectionId);
    if (results.Contains(AcceptDrawOfferResults.NoOfferToAccept))
    {
      return;
    }

    _gameRepository.RemoveByGameCode(game.GameCode);

    var (hostSecondsElapsed, guestSecondsElapsed) = _gameTimerService.RemoveTimers(game.GameCode);

    await _mediator.Publish(
      new GameOverCommand(
        game,
        GameOverReason.DrawByAgreement,
        hostSecondsElapsed,
        guestSecondsElapsed
      ),
      cancellationToken
    );
  }
}
