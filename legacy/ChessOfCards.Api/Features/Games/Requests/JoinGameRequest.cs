namespace ChessOfCards.Api.Features.Games;

public class JoinGameRequest(string gameCode, string? guestName)
{
  public string GameCode { get; set; } = gameCode;

  public string? GuestName { get; set; } = guestName;
}
