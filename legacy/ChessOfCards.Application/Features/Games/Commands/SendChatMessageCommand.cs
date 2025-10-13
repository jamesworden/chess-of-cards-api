using MediatR;

namespace ChessOfCards.Application.Features.Games;

public record SendChatMessageCommand(string ConnectionId, string RawMessage) : INotification
{
  public string ConnectionId { get; } = ConnectionId;

  public string RawMessage { get; } = RawMessage;
}
