namespace ChessOfCards.Domain.Features.Games;

public enum GameOverReason
{
    DrawByAgreement,
    Disconnected,
    Won,
    Resigned,
    RanOutOfTime,
    DrawByRepetition,
}
