using MediatR;

namespace ChessOfCards.GameActionHandler.Application.Features.Games.Commands;

public record ResignGameCommand(string ConnectionId) : INotification
{
    public string ConnectionId { get; } = ConnectionId;
}
