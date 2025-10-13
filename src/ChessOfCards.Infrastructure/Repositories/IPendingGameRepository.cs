using ChessOfCards.Infrastructure.Models;

namespace ChessOfCards.Infrastructure.Repositories;

/// <summary>
/// Repository interface for managing pending games
/// </summary>
public interface IPendingGameRepository
{
    /// <summary>
    /// Create a new pending game
    /// </summary>
    Task<PendingGameRecord> CreateAsync(PendingGameRecord game);

    /// <summary>
    /// Get a pending game by game code
    /// </summary>
    Task<PendingGameRecord?> GetByGameCodeAsync(string gameCode);

    /// <summary>
    /// Get a pending game by host connection ID
    /// </summary>
    Task<PendingGameRecord?> GetByHostConnectionIdAsync(string hostConnectionId);

    /// <summary>
    /// Get all pending games (for lobby list)
    /// </summary>
    Task<List<PendingGameRecord>> GetAllAsync();

    /// <summary>
    /// Delete a pending game
    /// </summary>
    Task<bool> DeleteAsync(string gameCode);
}
