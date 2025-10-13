namespace ChessOfCards.Domain.Features.Games;

public class Hand(List<Card> cards)
{
  public List<Card> Cards { get; set; } = cards;

  public void AddCard(Card card)
  {
    Cards.Add(card);
  }
}
