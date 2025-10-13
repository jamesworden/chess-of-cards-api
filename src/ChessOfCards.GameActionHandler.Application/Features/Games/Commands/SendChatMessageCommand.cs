using MediatR;

namespace ChessOfCards.GameActionHandler.Application.Features.Games.Commands;

public record SendChatMessageCommand(string ConnectionId, string RawMessage) : INotification
{
    public string ConnectionId { get; } = ConnectionId;

    public string RawMessage { get; } = RawMessage;
}
