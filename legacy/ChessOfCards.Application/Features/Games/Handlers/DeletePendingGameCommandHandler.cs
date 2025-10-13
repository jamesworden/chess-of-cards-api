using ChessOfCards.DataAccess.Interfaces;
using MediatR;

namespace ChessOfCards.Application.Features.Games;

public class DeletePendingGameCommandHandler(IPendingGameRepository pendingGameRepository)
  : INotificationHandler<DeletePendingGameCommand>
{
  private readonly IPendingGameRepository _pendingGameRepository = pendingGameRepository;

  public async Task Handle(DeletePendingGameCommand command, CancellationToken cancellationToken)
  {
    var removedPendingGame = _pendingGameRepository.RemoveByConnectionId(command.ConnectionId);
    if (removedPendingGame)
    {
      return;
    }

    await Task.CompletedTask;
  }
}
