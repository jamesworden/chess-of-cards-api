using ChessOfCards.Domain.Features.Games;
using MediatR;

namespace ChessOfCards.GameActionHandler.Application.Features.Games.Commands;

public record MakeMoveCommand(string ConnectionId, Move Move, List<Card>? RearrangedCardsInHand)
    : INotification
{
    public string ConnectionId { get; } = ConnectionId;

    public Move Move { get; } = Move;

    public List<Card>? RearrangedCardsInHand { get; } = RearrangedCardsInHand;
}
