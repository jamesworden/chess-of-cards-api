using ChessOfCards.Domain.Features.Games;

namespace ChessOfCards.Domain.Shared.Util;

public static class DurationOptionUtil
{
  public static int ToMinutes(this DurationOption durationOption)
  {
    return durationOption switch
    {
      DurationOption.FiveMinutes => 5,
      DurationOption.ThreeMinutes => 3,
      DurationOption.OneMinute => 1,
      _ => 0,
    };
  }

  public static int ToSeconds(this DurationOption durationOption)
  {
    return durationOption.ToMinutes() * 60;
  }
}
