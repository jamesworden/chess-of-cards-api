using ChessOfCards.Application.Features.Games;
using ChessOfCards.Application.Models;
using MediatR;
using Microsoft.AspNetCore.SignalR;

namespace ChessOfCards.Api.Features.Games;

public class PlayerReconnectedCommandHandler(IHubContext<GameHub> hubContext)
  : INotificationHandler<PlayerReconnectedCommand>
{
  private readonly IHubContext<GameHub> _hubContext = hubContext;

  public async Task Handle(PlayerReconnectedCommand command, CancellationToken cancellationToken)
  {
    await _hubContext
      .Clients.Client(command.ConnectionId)
      .SendAsync(
        NotificationType.PlayerReconnected,
        command.PlayerGameView,
        cancellationToken: cancellationToken
      );
  }
}
