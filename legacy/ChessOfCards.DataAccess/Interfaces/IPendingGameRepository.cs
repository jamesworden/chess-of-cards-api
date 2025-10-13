using ChessOfCards.Domain.Features.Games;

namespace ChessOfCards.DataAccess.Interfaces;

public interface IPendingGameRepository
{
  public PendingGame? FindByGameCode(string gameCode);

  public PendingGame? FindByConnectionId(string connectionId);

  public void Add(PendingGame pendingGame);

  public bool RemoveByGameCode(string gameCode);

  public bool RemoveByConnectionId(string connectionId);
}
