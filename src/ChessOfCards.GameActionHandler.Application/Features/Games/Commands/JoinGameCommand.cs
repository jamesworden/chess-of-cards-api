using MediatR;

namespace ChessOfCards.GameActionHandler.Application.Features.Games.Commands;

public record JoinGameCommand(string ConnectionId, string GameCode, string? GuestName) : IRequest
{
    public string ConnectionId { get; } = ConnectionId;
    public string GameCode { get; } = GameCode;
    public string? GuestName { get; } = GuestName;
}
