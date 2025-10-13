namespace ChessOfCards.Api.Features.Games;

public class SendChatMessageRequest(string RawMessage)
{
  public string RawMessage { get; set; } = RawMessage;
}
