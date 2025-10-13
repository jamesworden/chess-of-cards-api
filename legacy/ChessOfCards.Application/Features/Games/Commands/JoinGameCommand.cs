using MediatR;

namespace ChessOfCards.Application.Features.Games;

public record JoinGameCommand(string ConnectionId, string GameCode, string? Name) : IRequest
{
  public string ConnectionId { get; } = ConnectionId;

  public string GameCode { get; } = GameCode;

  public string? Name { get; } = Name;
}
