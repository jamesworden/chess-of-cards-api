using MediatR;

namespace ChessOfCards.GameActionHandler.Application.Features.Games.Commands;

public record PassMoveCommand(string ConnectionId) : IRequest
{
    public string ConnectionId { get; } = ConnectionId;
}
