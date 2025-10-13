using ChessOfCards.Infrastructure.Models;

namespace ChessOfCards.Infrastructure.Repositories;

/// <summary>
/// Repository interface for managing WebSocket connections
/// </summary>
public interface IConnectionRepository
{
    /// <summary>
    /// Create a new connection record
    /// </summary>
    Task<ConnectionRecord> CreateAsync(ConnectionRecord connection);

    /// <summary>
    /// Get a connection by connection ID
    /// </summary>
    Task<ConnectionRecord?> GetByConnectionIdAsync(string connectionId);

    /// <summary>
    /// Get all connections for a specific game
    /// </summary>
    Task<List<ConnectionRecord>> GetByGameCodeAsync(string gameCode);

    /// <summary>
    /// Update an existing connection
    /// </summary>
    Task<ConnectionRecord> UpdateAsync(ConnectionRecord connection);

    /// <summary>
    /// Delete a connection
    /// </summary>
    Task<bool> DeleteAsync(string connectionId);
}
