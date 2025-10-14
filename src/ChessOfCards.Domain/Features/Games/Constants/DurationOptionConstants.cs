namespace ChessOfCards.Domain.Features.Games.Constants;

/// <summary>
/// Constants for duration option string values.
/// These match the TypeScript enum values on the client side.
/// </summary>
public static class DurationOptionConstants
{
    public const string FiveMinutes = "FiveMinutes";
    public const string ThreeMinutes = "ThreeMinutes";
    public const string OneMinute = "OneMinute";

    /// <summary>
    /// Default duration option if none is specified.
    /// </summary>
    public const string Default = ThreeMinutes;

    /// <summary>
    /// All valid duration option values.
    /// </summary>
    public static readonly string[] ValidOptions = [FiveMinutes, ThreeMinutes, OneMinute];
}
