using MediatR;

namespace ChessOfCards.Application.Features.Games;

public record GameClockTimerExpiredCommand(
  string GameCode,
  double HostSecondsElapsed,
  double GuestSecondsElapsed
) : INotification
{
  public string GameCode { get; } = GameCode;

  public double HostSecondsElapsed { get; } = HostSecondsElapsed;

  public double GuestSecondsElapsed { get; } = GuestSecondsElapsed;
}
