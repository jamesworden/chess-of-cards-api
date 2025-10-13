namespace ChessOfCards.Domain.Features.Games;

public class ChatMessageView(string message, PlayerOrNone sentBy, DateTime sentAtUTC)
{
    public DateTime SentAtUTC { get; set; } = sentAtUTC;

    public PlayerOrNone SentBy { get; set; } = sentBy;

    public string Message { get; set; } = message;
}
