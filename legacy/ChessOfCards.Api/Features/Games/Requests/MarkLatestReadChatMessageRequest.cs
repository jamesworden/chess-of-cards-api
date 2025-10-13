namespace ChessOfCards.Api.Features.Games;

public class MarkLatestReadChatMessageRequest(int LatestIndex)
{
  public int LatestIndex { get; set; } = LatestIndex;
}
