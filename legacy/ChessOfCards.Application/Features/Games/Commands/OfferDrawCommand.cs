using MediatR;

namespace ChessOfCards.Application.Features.Games;

public record OfferDrawCommand(string ConnectionId) : INotification
{
  public string ConnectionId { get; } = ConnectionId;
}
