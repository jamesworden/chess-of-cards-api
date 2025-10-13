using ChessOfCards.Domain.Features.Games;
using MediatR;

namespace ChessOfCards.GameActionHandler.Application.Features.Games.Commands;

public record RearrangeHandCommand(string ConnectionId, List<Card> Cards) : INotification
{
    public string ConnectionId { get; } = ConnectionId;

    public List<Card> Cards { get; } = Cards;
}
