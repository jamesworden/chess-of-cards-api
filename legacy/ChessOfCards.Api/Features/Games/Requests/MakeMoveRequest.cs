using ChessOfCards.Domain.Features.Games;

namespace ChessOfCards.Api.Features.Games;

public class MakeMoveRequest(Move Move, List<Card>? RearrangedCardsInHand)
{
  public Move Move { get; set; } = Move;

  public List<Card>? RearrangedCardsInHand { get; set; } = RearrangedCardsInHand;
}
