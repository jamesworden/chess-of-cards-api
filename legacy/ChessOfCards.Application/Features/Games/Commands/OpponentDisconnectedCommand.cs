using MediatR;

namespace ChessOfCards.Application.Features.Games;

public record OpponentDisconnectedCommand(string ConnectionId) : INotification
{
  public string ConnectionId { get; } = ConnectionId;
}
