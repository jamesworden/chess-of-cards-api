using ChessOfCards.Domain.Features.Games;
using MediatR;

namespace ChessOfCards.Application.Features.Games;

public record CreatePendingGameCommand(
  string ConnectionId,
  DurationOption DurationOption,
  string? HostName
) : IRequest
{
  public string ConnectionId { get; } = ConnectionId;

  public DurationOption DurationOption { get; } = DurationOption;

  public string? HostName { get; } = HostName;
}
