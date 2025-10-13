using ChessOfCards.Domain.Features.Games;
using MediatR;

namespace ChessOfCards.Application.Features.Games;

public record GameStartedCommand(Game Game) : INotification
{
  public Game Game { get; } = Game;
}
