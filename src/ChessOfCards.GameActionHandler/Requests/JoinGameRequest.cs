using ChessOfCards.GameActionHandler.Application.Features.Games.Commands;

namespace ChessOfCards.GameActionHandler.Requests;

public class JoinGameRequest
{
    public string GameCode { get; set; } = string.Empty;
    public string? GuestName { get; set; }

    public JoinGameCommand ToCommand(string connectionId)
    {
        return new JoinGameCommand(connectionId, GameCode, GuestName);
    }
}
