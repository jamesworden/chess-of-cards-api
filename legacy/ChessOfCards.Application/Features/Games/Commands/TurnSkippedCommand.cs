using ChessOfCards.Domain.Features.Games;
using MediatR;

namespace ChessOfCards.Application.Features.Games;

public record TurnSkippedCommand(string ConnectionId, PlayerGameView PlayerGameView) : INotification
{
  public string ConnectionId { get; } = ConnectionId;

  public PlayerGameView PlayerGameView { get; } = PlayerGameView;
}
