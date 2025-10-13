using ChessOfCards.Application.Features.Games;
using ChessOfCards.Application.Models;
using MediatR;
using Microsoft.AspNetCore.SignalR;

namespace ChessOfCards.Api.Features.Games;

public class GameOverCommandHandler(IHubContext<GameHub> hubContext)
  : INotificationHandler<GameOverCommand>
{
  private readonly IHubContext<GameHub> _hubContext = hubContext;

  public async Task Handle(GameOverCommand command, CancellationToken cancellationToken)
  {
    await Task.WhenAll(
      [
        _hubContext
          .Clients.Client(command.Game.HostConnectionId)
          .SendAsync(
            NotificationType.GameOver,
            command.Game.ToHostPlayerView(command.HostSecondsElapsed, command.GuestSecondsElapsed),
            command.GameOverReason,
            cancellationToken: cancellationToken
          ),
        _hubContext
          .Clients.Client(command.Game.GuestConnectionId)
          .SendAsync(
            NotificationType.GameOver,
            command.Game.ToGuestPlayerView(command.HostSecondsElapsed, command.GuestSecondsElapsed),
            command.GameOverReason,
            cancellationToken: cancellationToken
          )
      ]
    );
  }
}
