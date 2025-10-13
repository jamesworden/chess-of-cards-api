using MediatR;

namespace ChessOfCards.GameActionHandler.Application.Features.Games.Commands;

public record CreatePendingGameCommand(
    string ConnectionId,
    string? DurationOption,
    string? HostName
) : IRequest
{
    public string ConnectionId { get; } = ConnectionId;
    public string? DurationOption { get; } = DurationOption;
    public string? HostName { get; } = HostName;
}
