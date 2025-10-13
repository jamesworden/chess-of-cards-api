using ChessOfCards.GameActionHandler.Application.Features.Games.Commands;

namespace ChessOfCards.GameActionHandler.Requests;

public class MarkLatestReadChatMessageRequest
{
    public int LatestIndex { get; set; }

    public MarkLatestReadChatMessageCommand ToCommand(string connectionId)
    {
        return new MarkLatestReadChatMessageCommand(connectionId, LatestIndex);
    }
}
