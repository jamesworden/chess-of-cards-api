using ChessOfCards.Domain.Features.Games;
using MediatR;

namespace ChessOfCards.Application.Features.Games;

public record GameOverCommand(
  Game Game,
  GameOverReason GameOverReason,
  double HostSecondsElapsed,
  double GuestSecondsElapsed
) : INotification
{
  public Game Game { get; } = Game;

  public GameOverReason GameOverReason { get; } = GameOverReason;

  public double HostSecondsElapsed { get; } = HostSecondsElapsed;

  public double GuestSecondElapsed { get; } = GuestSecondsElapsed;
}
