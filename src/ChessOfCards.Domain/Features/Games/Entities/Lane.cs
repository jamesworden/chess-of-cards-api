namespace ChessOfCards.Domain.Features.Games;

public class Lane(List<Card>[] rows)
{
    public List<Card>[] Rows { get; set; } = rows;

    public PlayerOrNone LaneAdvantage { get; set; } = PlayerOrNone.None;

    public Card? LastCardPlayed { get; set; }

    public PlayerOrNone WonBy { get; set; } = PlayerOrNone.None;
}
