using MediatR;

namespace ChessOfCards.Application.Features.Games;

public record PlayerDisconnectedCommand(string ConnectionId) : INotification
{
  public string ConnectionId { get; } = ConnectionId;
}
