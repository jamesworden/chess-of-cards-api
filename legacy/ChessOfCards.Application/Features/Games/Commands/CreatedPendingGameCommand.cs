using ChessOfCards.Domain.Features.Games;
using MediatR;

namespace ChessOfCards.Application.Features.Games;

public record CreatedPendingGameCommand(string ConnectionId, PendingGameView PendingGameView)
  : INotification
{
  public string ConnectionId = ConnectionId;

  public PendingGameView PendingGameView { get; } = PendingGameView;
}
