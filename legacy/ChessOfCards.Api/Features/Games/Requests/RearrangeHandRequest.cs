using ChessOfCards.Domain.Features.Games;

namespace ChessOfCards.Api.Features.Games;

public class RearrangeHandRequest(List<Card> Cards)
{
  public List<Card> Cards { get; set; } = Cards;
}
