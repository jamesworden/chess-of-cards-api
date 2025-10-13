using ChessOfCards.Application.Features.Games;
using ChessOfCards.Application.Models;
using MediatR;
using Microsoft.AspNetCore.SignalR;

namespace ChessOfCards.Api.Features.Games;

public class GameStartedCommandHandler(IHubContext<GameHub> hubContext)
  : INotificationHandler<GameStartedCommand>
{
  private readonly IHubContext<GameHub> _hubContext = hubContext;

  public async Task Handle(GameStartedCommand command, CancellationToken cancellationToken)
  {
    await Task.WhenAll(
      [
        _hubContext
          .Clients.Client(command.Game.HostConnectionId)
          .SendAsync(
            NotificationType.GameStarted,
            command.Game.ToHostPlayerView(0, 0),
            cancellationToken: cancellationToken
          ),
        _hubContext
          .Clients.Client(command.Game.GuestConnectionId)
          .SendAsync(
            NotificationType.GameStarted,
            command.Game.ToGuestPlayerView(0, 0),
            cancellationToken: cancellationToken
          ),
      ]
    );
  }
}
