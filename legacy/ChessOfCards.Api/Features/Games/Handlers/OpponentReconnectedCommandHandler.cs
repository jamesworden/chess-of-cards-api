using ChessOfCards.Application.Features.Games;
using ChessOfCards.Application.Models;
using MediatR;
using Microsoft.AspNetCore.SignalR;

namespace ChessOfCards.Api.Features.Games;

public class OpponentReconnectedCommandHandler(IHubContext<GameHub> hubContext)
  : INotificationHandler<OpponentReconnectedCommand>
{
  private readonly IHubContext<GameHub> _hubContext = hubContext;

  public async Task Handle(OpponentReconnectedCommand command, CancellationToken cancellationToken)
  {
    await _hubContext
      .Clients.Client(command.ConnectionId)
      .SendAsync(NotificationType.OpponentReconnected, cancellationToken: cancellationToken);
  }
}
