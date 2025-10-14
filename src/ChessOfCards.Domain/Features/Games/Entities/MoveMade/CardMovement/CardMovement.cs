namespace ChessOfCards.Domain.Features.Games;

public class CardMovement
{
    public CardStore From { get; set; }

    public CardStore To { get; set; }

    public Card? Card { get; set; }

    public string? Notation { get; set; }

    // Parameterless constructor for JSON deserialization
    public CardMovement()
    {
        From = new CardStore();
        To = new CardStore();
    }

    public CardMovement(CardStore from, CardStore to, Card? card)
    {
        From = from;
        To = to;
        Card = card;
    }

    public CardMovement(CardStore from, CardStore to, Card? card, string? notation)
    {
        From = from;
        To = to;
        Card = card;
        Notation = notation;
    }
}
