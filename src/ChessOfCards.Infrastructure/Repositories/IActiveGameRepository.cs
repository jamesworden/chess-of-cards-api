using ChessOfCards.Infrastructure.Models;

namespace ChessOfCards.Infrastructure.Repositories;

/// <summary>
/// Repository interface for managing active games
/// </summary>
public interface IActiveGameRepository
{
    /// <summary>
    /// Create a new active game
    /// </summary>
    Task<ActiveGameRecord> CreateAsync(ActiveGameRecord game);

    /// <summary>
    /// Get a game by game code
    /// </summary>
    Task<ActiveGameRecord?> GetByGameCodeAsync(string gameCode);

    /// <summary>
    /// Get a game by connection ID (checks both host and guest)
    /// </summary>
    Task<ActiveGameRecord?> GetByConnectionIdAsync(string connectionId);

    /// <summary>
    /// Update an existing game with optimistic locking
    /// </summary>
    Task<ActiveGameRecord?> UpdateAsync(ActiveGameRecord game);

    /// <summary>
    /// Delete a game
    /// </summary>
    Task<bool> DeleteAsync(string gameCode);
}
