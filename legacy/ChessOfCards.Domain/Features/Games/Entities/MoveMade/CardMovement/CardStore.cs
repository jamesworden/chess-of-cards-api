namespace ChessOfCards.Domain.Features.Games;

/// <summary>
/// Where a card is in the game. Only one property should be truthy.
/// </summary>
public class CardStore
{
  public int? HostHandCardIndex { get; set; } = null;

  public int? GuestHandCardIndex { get; set; } = null;

  public CardPosition? CardPosition { get; set; } = null;

  public bool Destroyed { get; set; } = false;

  public bool HostDeck { get; set; } = false;

  public bool GuestDeck { get; set; } = false;

  public CardStore() { }
}
