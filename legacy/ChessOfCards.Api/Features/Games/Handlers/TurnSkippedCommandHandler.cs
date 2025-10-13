using ChessOfCards.Application.Features.Games;
using ChessOfCards.Application.Models;
using MediatR;
using Microsoft.AspNetCore.SignalR;

namespace ChessOfCards.Api.Features.Games;

public class TurnSkippedCommandHandler(IHubContext<GameHub> hubContext)
  : INotificationHandler<TurnSkippedCommand>
{
  private readonly IHubContext<GameHub> _hubContext = hubContext;

  public async Task Handle(TurnSkippedCommand command, CancellationToken cancellationToken)
  {
    await _hubContext
      .Clients.Client(command.ConnectionId)
      .SendAsync(
        NotificationType.TurnSkipped,
        command.PlayerGameView,
        cancellationToken: cancellationToken
      );
  }
}
