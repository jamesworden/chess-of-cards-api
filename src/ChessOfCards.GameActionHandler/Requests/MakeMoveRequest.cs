using ChessOfCards.Domain.Features.Games;
using ChessOfCards.GameActionHandler.Application.Features.Games.Commands;

namespace ChessOfCards.GameActionHandler.Requests;

public class MakeMoveRequest
{
    public Move Move { get; set; } = null!;

    public List<Card>? RearrangedCardsInHand { get; set; }

    public MakeMoveCommand ToCommand(string connectionId)
    {
        return new MakeMoveCommand(connectionId, Move, RearrangedCardsInHand);
    }
}
