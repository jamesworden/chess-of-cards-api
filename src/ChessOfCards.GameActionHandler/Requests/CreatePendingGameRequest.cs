using ChessOfCards.GameActionHandler.Application.Features.Games.Commands;

namespace ChessOfCards.GameActionHandler.Requests;

public class CreatePendingGameRequest
{
    public string? DurationOption { get; set; }
    public string? HostName { get; set; }

    public CreatePendingGameCommand ToCommand(string connectionId)
    {
        return new CreatePendingGameCommand(connectionId, DurationOption, HostName);
    }
}
