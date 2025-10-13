using ChessOfCards.GameActionHandler.Application.Features.Games.Commands;

namespace ChessOfCards.GameActionHandler.Requests;

public class SendChatMessageRequest
{
    public string RawMessage { get; set; } = string.Empty;

    public SendChatMessageCommand ToCommand(string connectionId)
    {
        return new SendChatMessageCommand(connectionId, RawMessage);
    }
}
