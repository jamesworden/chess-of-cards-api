using MediatR;

namespace ChessOfCards.Application.Features.Games;

public record PassMoveCommand(string ConnectionId) : IRequest
{
  public string ConnectionId { get; } = ConnectionId;
}
