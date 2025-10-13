namespace ChessOfCards.Domain.Features.Games;

public class CardPosition(int laneIndex, int rowIndex)
{
  public int LaneIndex { get; set; } = laneIndex;

  public int RowIndex { get; set; } = rowIndex;
}
