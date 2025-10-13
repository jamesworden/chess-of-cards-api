using ChessOfCards.DataAccess.Interfaces;
using ChessOfCards.Domain.Features.Games;

namespace ChessOfCards.DataAccess.Repositories;

public class GameRepository : IGameRepository
{
  private readonly List<Game> Games = [];

  public void Add(Game game)
  {
    Games.Add(game);
  }

  public Game? FindByConnectionId(string connectionId)
  {
    var gamesWithConnectionId = Games.Where(game =>
    {
      var hostConnectionIdMatches = game.HostConnectionId == connectionId;
      var guestConnectionIdMatches = game.GuestConnectionId == connectionId;

      return hostConnectionIdMatches || guestConnectionIdMatches;
    });

    var gameWithConnectionId = gamesWithConnectionId.FirstOrDefault();

    return gameWithConnectionId;
  }

  public Game? RemoveByConnectionId(string connectionId)
  {
    var game = FindByConnectionId(connectionId);

    if (game is not null)
    {
      Games.Remove(game);
    }

    return game;
  }

  public Game? FindByGameCode(string gameCode)
  {
    return Games.FirstOrDefault(game => game.GameCode == gameCode);
  }

  public void RemoveByGameCode(string gameCode)
  {
    Games.RemoveAll(game => game.GameCode == gameCode);
  }
}
