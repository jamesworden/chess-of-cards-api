using MediatR;

namespace ChessOfCards.GameActionHandler.Application.Features.Games.Commands;

public record DeletePendingGameCommand(string ConnectionId) : IRequest
{
    public string ConnectionId { get; } = ConnectionId;
}
