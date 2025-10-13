using MediatR;

namespace ChessOfCards.Application.Features.Games;

public record DrawOfferedCommand(string ConnectionId) : INotification
{
  public string ConnectionId { get; } = ConnectionId;
}
