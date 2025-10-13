namespace ChessOfCards.Domain.Features.Games;

public enum MakeMoveResults
{
  InvalidCards,
  InvalidMove,
  GuestTurnSkippedNoMoves,
  HostTurnSkippedNoMoves,
}
