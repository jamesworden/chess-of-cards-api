using MediatR;

namespace ChessOfCards.Application.Features.Games;

public record ResignGameCommand(string ConnectionId) : INotification
{
  public string ConnectionId { get; } = ConnectionId;
}
