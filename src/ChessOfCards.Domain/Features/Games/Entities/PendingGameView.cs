namespace ChessOfCards.Domain.Features.Games;

public class PendingGameView(string gameCode, DurationOption durationOption, string? hostName)
{
    public string GameCode { get; set; } = gameCode;

    public DurationOption DurationOption { get; set; } = durationOption;

    public string? HostName { get; set; } = hostName;
}
