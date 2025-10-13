using MediatR;

namespace ChessOfCards.Application.Features.Games;

public record JoinGameCodeInvalidCommand(string ConnectionId) : INotification
{
  public string ConnectionId { get; } = ConnectionId;
}
