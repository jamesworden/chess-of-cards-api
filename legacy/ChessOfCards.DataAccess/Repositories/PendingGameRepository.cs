using ChessOfCards.DataAccess.Interfaces;
using ChessOfCards.Domain.Features.Games;

namespace ChessOfCards.DataAccess.Repositories;

public class PendingGameRepository : IPendingGameRepository
{
  private static readonly Dictionary<string, PendingGame> PendingGameCodeToHostConnectionId = [];

  public void Add(PendingGame pendingGame)
  {
    PendingGameCodeToHostConnectionId.Add(pendingGame.GameCode, pendingGame);
  }

  public PendingGame? FindByConnectionId(string connectionId)
  {
    return PendingGameCodeToHostConnectionId
        .FirstOrDefault(row => row.Value is not null && row.Value.HostConnectionId == connectionId)
        .Value ?? null;
  }

  public PendingGame? FindByGameCode(string gameCode)
  {
    PendingGameCodeToHostConnectionId.TryGetValue(gameCode, out var pendingGame);
    return pendingGame;
  }

  public bool RemoveByGameCode(string gameCode)
  {
    return PendingGameCodeToHostConnectionId.Remove(gameCode);
  }

  public bool RemoveByConnectionId(string connectionId)
  {
    var entries = PendingGameCodeToHostConnectionId.ToList();
    var numRemoved = entries.RemoveAll(keyValuePair =>
      keyValuePair.Value.HostConnectionId == connectionId
    );
    return numRemoved > 0;
  }
}
