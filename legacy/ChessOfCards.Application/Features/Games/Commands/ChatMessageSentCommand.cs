using ChessOfCards.Domain.Features.Games;
using MediatR;

namespace ChessOfCards.Application.Features.Games;

public record ChatMessageSentCommand(
  Game Game,
  double HostSecondsElapsed,
  double GuestSecondsElapsed
) : INotification
{
  public Game Game { get; } = Game;

  public double HostSecondsElapsed { get; } = HostSecondsElapsed;

  public double GuestSecondsElapsed { get; } = GuestSecondsElapsed;
}
