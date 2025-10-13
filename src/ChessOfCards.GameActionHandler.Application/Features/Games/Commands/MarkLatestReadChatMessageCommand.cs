using MediatR;

namespace ChessOfCards.GameActionHandler.Application.Features.Games.Commands;

public record MarkLatestReadChatMessageCommand(string ConnectionId, int LatestIndex) : INotification
{
    public string ConnectionId { get; } = ConnectionId;

    public int LatestIndex { get; } = LatestIndex;
}
