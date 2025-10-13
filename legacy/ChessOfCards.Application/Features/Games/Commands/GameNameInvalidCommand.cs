using MediatR;

namespace ChessOfCards.Application.Features.Games;

public record GameNameInvalidCommand(string ConnectionId) : INotification
{
  public string ConnectionId { get; } = ConnectionId;
}
