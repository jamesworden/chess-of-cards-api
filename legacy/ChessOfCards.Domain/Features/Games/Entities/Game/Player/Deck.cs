namespace ChessOfCards.Domain.Features.Games;

public class Deck
{
  public List<Card> Cards { get; set; }

  public Deck(List<Card> cards)
  {
    Cards = cards;
  }

  public Deck()
  {
    var cards = new List<Card>();
    var suits = Enum.GetValues(typeof(Suit));

    foreach (Suit suit in suits)
    {
      var kinds = Enum.GetValues(typeof(Kind));
      foreach (Kind kind in kinds)
      {
        var card = new Card(kind, suit);
        cards.Add(card);
      }
    }

    Cards = cards;
  }

  public Deck Shuffle()
  {
    Random random = new();
    Cards = [.. Cards.OrderBy(card => random.Next())];
    return this;
  }

  public Tuple<Deck, Deck> Split()
  {
    var numCardsInHalfDeck = Cards.Count / 2;

    var firstDeckCards = DrawCards(numCardsInHalfDeck);
    var firstDeck = new Deck(firstDeckCards);

    var secondDeckCards = DrawRemainingCards();
    var secondDeck = new Deck(secondDeckCards);

    return new Tuple<Deck, Deck>(firstDeck, secondDeck);
  }

  public List<Card> DrawCards(int numberOfCards)
  {
    List<Card> cards = [];

    if (numberOfCards > Cards.Count)
    {
      numberOfCards = Cards.Count;
    }

    for (int i = 0; i < numberOfCards; i++)
    {
      var card = Cards.ElementAt(i);
      Cards.RemoveAt(i);
      cards.Add(card);
    }

    return cards;
  }

  public Card? DrawCard()
  {
    List<Card> singleCardList = DrawCards(1);
    if (singleCardList.Count != 1)
    {
      return null;
    }
    var card = singleCardList.ElementAt(0);
    return card;
  }

  public List<Card> DrawRemainingCards()
  {
    List<Card> remainingCards = new(Cards);
    Cards.Clear();
    return remainingCards;
  }
}
