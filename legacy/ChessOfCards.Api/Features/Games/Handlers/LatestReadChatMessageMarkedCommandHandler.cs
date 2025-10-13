using ChessOfCards.Application.Features.Games;
using ChessOfCards.Application.Models;
using MediatR;
using Microsoft.AspNetCore.SignalR;

namespace ChessOfCards.Api.Features.Games;

public class LatestReadChatMessageMarkedCommandHandler(IHubContext<GameHub> hubContext)
  : INotificationHandler<LatestReadChatMessageMarkedCommand>
{
  private readonly IHubContext<GameHub> _hubContext = hubContext;

  public async Task Handle(
    LatestReadChatMessageMarkedCommand command,
    CancellationToken cancellationToken
  )
  {
    await Task.WhenAll(
      [
        _hubContext
          .Clients.Client(command.Game.HostConnectionId)
          .SendAsync(
            NotificationType.LatestReadChatMessageMarked,
            command.Game.ToHostPlayerView(command.HostSecondsElapsed, command.GuestSecondsElapsed),
            cancellationToken: cancellationToken
          ),
        _hubContext
          .Clients.Client(command.Game.GuestConnectionId)
          .SendAsync(
            NotificationType.LatestReadChatMessageMarked,
            command.Game.ToGuestPlayerView(command.HostSecondsElapsed, command.GuestSecondsElapsed),
            cancellationToken: cancellationToken
          )
      ]
    );
  }
}
