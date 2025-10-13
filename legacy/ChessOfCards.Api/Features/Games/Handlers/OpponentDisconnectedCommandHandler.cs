using ChessOfCards.Application.Features.Games;
using ChessOfCards.Application.Models;
using MediatR;
using Microsoft.AspNetCore.SignalR;

namespace ChessOfCards.Api.Features.Games;

public class OpponentDisconnectedCommandHandler(IHubContext<GameHub> hubContext)
  : INotificationHandler<OpponentDisconnectedCommand>
{
  private readonly IHubContext<GameHub> _hubContext = hubContext;

  public async Task Handle(OpponentDisconnectedCommand command, CancellationToken cancellationToken)
  {
    await _hubContext
      .Clients.Client(command.ConnectionId)
      .SendAsync(NotificationType.OpponentDisconnected, cancellationToken: cancellationToken);
  }
}
