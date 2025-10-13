using ChessOfCards.Application.Features.Games;
using ChessOfCards.Application.Models;
using MediatR;
using Microsoft.AspNetCore.SignalR;

namespace ChessOfCards.Api.Features.Games;

public class GameUpdatedCommandHandler(IHubContext<GameHub> hubContext)
  : INotificationHandler<GameUpdatedCommand>
{
  private readonly IHubContext<GameHub> _hubContext = hubContext;

  public async Task Handle(GameUpdatedCommand command, CancellationToken cancellationToken)
  {
    await Task.WhenAll(
      [
        _hubContext
          .Clients.Client(command.Game.HostConnectionId)
          .SendAsync(
            NotificationType.GameUpdated,
            command.Game.ToHostPlayerView(command.HostSecondsElapsed, command.GuestSecondsElapsed),
            cancellationToken: cancellationToken
          ),
        _hubContext
          .Clients.Client(command.Game.GuestConnectionId)
          .SendAsync(
            NotificationType.GameUpdated,
            command.Game.ToGuestPlayerView(command.HostSecondsElapsed, command.GuestSecondsElapsed),
            cancellationToken: cancellationToken
          )
      ]
    );
  }
}
