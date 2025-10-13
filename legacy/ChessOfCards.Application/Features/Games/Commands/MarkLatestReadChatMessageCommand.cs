using MediatR;

namespace ChessOfCards.Application.Features.Games;

public record MarkLatestReadChatMessageCommand(string ConnectionId, int LatestIndex) : INotification
{
  public string ConnectionId { get; } = ConnectionId;

  public int LatestIndex { get; } = LatestIndex;
}
