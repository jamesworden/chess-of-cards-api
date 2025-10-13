using ChessOfCards.Domain.Features.Games;
using ChessOfCards.GameActionHandler.Application.Features.Games.Commands;

namespace ChessOfCards.GameActionHandler.Requests;

public class RearrangeHandRequest
{
    public List<Card> Cards { get; set; } = null!;

    public RearrangeHandCommand ToCommand(string connectionId)
    {
        return new RearrangeHandCommand(connectionId, Cards);
    }
}
