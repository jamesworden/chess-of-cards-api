using ChessOfCards.Domain.Features.Games;

namespace ChessOfCards.DataAccess.Interfaces;

public interface IGameRepository
{
  public Game? FindByGameCode(string gameCode);

  public void Add(Game game);

  public Game? FindByConnectionId(string connectionId);

  public void RemoveByGameCode(string gameCode);
}
