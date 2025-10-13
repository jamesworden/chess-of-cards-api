using MediatR;

namespace ChessOfCards.Application.Features.Games;

public record DisconnectTimerExpiredCommand(string GameCode) : INotification
{
  public string GameCode { get; } = GameCode;
}
