namespace ChessOfCards.Domain.Features.Games;

public class PlaceCardAttempt(Card card, int targetLaneIndex, int targetRowIndex)
{
  public Card Card { get; set; } = card;

  public int TargetLaneIndex { get; set; } = targetLaneIndex;

  public int TargetRowIndex { get; set; } = targetRowIndex;

  public bool IsDefensive(bool playerIsHost)
  {
    return (TargetRowIndex < 3 && playerIsHost) || (TargetRowIndex > 3 && !playerIsHost);
  }
}
