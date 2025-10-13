using MediatR;

namespace ChessOfCards.Application.Features.Games;

public record DeletePendingGameCommand(string ConnectionId) : INotification
{
  public string ConnectionId { get; } = ConnectionId;
}
