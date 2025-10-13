namespace ChessOfCards.Domain.Features.Games;

public class CandidateMove(Move move, bool isValid, string? invalidReason)
{
    public Move Move { get; set; } = move;

    public bool IsValid { get; set; } = isValid;

    public string? InvalidReason { get; set; } = invalidReason;
}
