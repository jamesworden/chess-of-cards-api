using ChessOfCards.DataAccess.Interfaces;
using ChessOfCards.Domain.Features.Games;
using MediatR;

namespace ChessOfCards.Application.Features.Games;

public class OfferDrawCommandHandler(IGameRepository gameRepository, IMediator mediator)
  : INotificationHandler<OfferDrawCommand>
{
  private readonly IGameRepository _gameRepository = gameRepository;

  private readonly IMediator _mediator = mediator;

  public async Task Handle(OfferDrawCommand command, CancellationToken cancellationToken)
  {
    var game = _gameRepository.FindByConnectionId(command.ConnectionId);
    if (game is null)
    {
      return;
    }

    var results = game.OfferDraw(command.ConnectionId);
    if (results.Contains(OfferDrawResults.AlreadyOfferedDraw))
    {
      return;
    }

    var opponentConnectionId =
      game.HostConnectionId == command.ConnectionId
        ? game.GuestConnectionId
        : game.HostConnectionId;

    await _mediator.Publish(new DrawOfferedCommand(opponentConnectionId), cancellationToken);
  }
}
