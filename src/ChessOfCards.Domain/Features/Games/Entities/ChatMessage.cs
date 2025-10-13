namespace ChessOfCards.Domain.Features.Games;

public class ChatMessage(
    string rawMessage,
    string sensoredMessage,
    DateTime sentAtUTC,
    PlayerOrNone sentBy
)
{
    public string RawMessage { get; set; } = rawMessage;

    public string SensoredMessage { get; set; } = sensoredMessage;

    public DateTime SentAtUtc { get; set; } = sentAtUTC;

    public PlayerOrNone SentBy { get; set; } = sentBy;
}
