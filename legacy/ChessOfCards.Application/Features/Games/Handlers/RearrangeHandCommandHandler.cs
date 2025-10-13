using ChessOfCards.DataAccess.Interfaces;
using MediatR;

namespace ChessOfCards.Application.Features.Games;

public class RearrangeHandCommandHandler(IGameRepository gameRepository)
  : INotificationHandler<RearrangeHandCommand>
{
  private readonly IGameRepository _gameRepository = gameRepository;

  public Task Handle(RearrangeHandCommand command, CancellationToken cancellationToken)
  {
    var game = _gameRepository.FindByConnectionId(command.ConnectionId);
    if (game is null)
    {
      return Task.CompletedTask;
    }

    game.RearrangeHand(command.ConnectionId, command.Cards);

    return Task.CompletedTask;
  }
}
