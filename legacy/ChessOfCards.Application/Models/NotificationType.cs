namespace ChessOfCards.Application.Models;

public static class NotificationType
{
  public static readonly string GameStarted = "GameStarted";

  public static readonly string InvalidName = "InvalidName";

  public static readonly string CreatedPendingGame = "CreatedPendingGame";

  public static readonly string InvalidGameCode = "InvalidGameCode";

  public static readonly string PlayerReconnected = "PlayerReconnected";

  public static readonly string GameUpdated = "GameUpdated";

  public static readonly string OpponentDisconnected = "OpponentDisconnected";

  public static readonly string OpponentReconnected = "OpponentReconnected";

  public static readonly string TurnSkipped = "TurnSkipped";

  public static readonly string GameOver = "GameOver";

  public static readonly string DrawOffered = "DrawOffered";

  public static readonly string ChatMessageSent = "ChatMessageSent";

  public static readonly string LatestReadChatMessageMarked = "LatestReadChatMessageMarked";
}
