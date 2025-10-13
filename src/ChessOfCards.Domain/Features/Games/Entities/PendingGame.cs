namespace ChessOfCards.Domain.Features.Games;

public class PendingGame(
    string gameCode,
    string hostConnectionId,
    DurationOption durationOption,
    string? hostName
)
{
    public string GameCode { get; set; } = gameCode;

    public string HostConnectionId { get; set; } = hostConnectionId;

    public DurationOption DurationOption { get; set; } = durationOption;

    public string? HostName { get; set; } = hostName;

    public PendingGameView ToPendingGameView()
    {
        return new PendingGameView(GameCode, DurationOption, HostName);
    }
}
