using ChessOfCards.Infrastructure.Models;

namespace ChessOfCards.Infrastructure.Repositories;

/// <summary>
/// Repository interface for managing game timers
/// </summary>
public interface IGameTimerRepository
{
    /// <summary>
    /// Create a new timer
    /// </summary>
    Task<GameTimerRecord> CreateAsync(GameTimerRecord timer);

    /// <summary>
    /// Get a timer by timer ID
    /// </summary>
    Task<GameTimerRecord?> GetByIdAsync(string timerId);

    /// <summary>
    /// Get all timers expiring before a given timestamp
    /// </summary>
    Task<List<GameTimerRecord>> GetExpiringTimersAsync(
        string timerType,
        long expiresBeforeTimestamp
    );

    /// <summary>
    /// Update an existing timer
    /// </summary>
    Task<GameTimerRecord> UpdateAsync(GameTimerRecord timer);

    /// <summary>
    /// Delete a timer
    /// </summary>
    Task<bool> DeleteAsync(string timerId);

    /// <summary>
    /// Delete all timers for a game
    /// </summary>
    Task<bool> DeleteAllForGameAsync(string gameCode);
}
