namespace ChessOfCards.Domain.Features.Games;

public class Player(Deck deck, Hand hand)
{
    public Hand Hand { get; set; } = hand;

    public Deck Deck { get; set; } = deck;
}
