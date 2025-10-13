using ChessOfCards.Domain.Features.Games;

namespace ChessOfCards.Api.Features.Games;

public class CreatePendingGameRequest(DurationOption durationOption, string? hostName)
{
  public DurationOption DurationOption { get; set; } = durationOption;

  public string? HostName { get; set; } = hostName;
}
