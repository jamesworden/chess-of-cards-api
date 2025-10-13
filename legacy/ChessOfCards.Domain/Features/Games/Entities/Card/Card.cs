namespace ChessOfCards.Domain.Features.Games;

public class Card(Kind kind, Suit suit, PlayerOrNone playedBy = PlayerOrNone.None)
{
  public Kind Kind { get; set; } = kind;

  public Suit Suit { get; set; } = suit;

  public PlayerOrNone PlayedBy { get; set; } = playedBy;

  public bool SuitAndKindMatch(Card card)
  {
    return SuitMatches(card) && KindMatches(card);
  }

  public bool SuitMatches(Card card)
  {
    return card.Suit == Suit;
  }

  public bool KindMatches(Card card)
  {
    return card.Kind == Kind;
  }

  public bool Trumps(Card defender)
  {
    return SuitMatches(defender) ? Kind > defender.Kind : KindMatches(defender);
  }

  public bool SuitOrKindMatch(Card card)
  {
    return SuitMatches(card) || KindMatches(card);
  }
}
