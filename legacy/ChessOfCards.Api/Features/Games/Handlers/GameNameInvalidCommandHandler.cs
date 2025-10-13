using ChessOfCards.Application.Features.Games;
using ChessOfCards.Application.Models;
using MediatR;
using Microsoft.AspNetCore.SignalR;

namespace ChessOfCards.Api.Features.Games;

public class GameNameInvalidCommandHandler(IHubContext<GameHub> hubContext)
  : INotificationHandler<GameNameInvalidCommand>
{
  private readonly IHubContext<GameHub> _hubContext = hubContext;

  public async Task Handle(GameNameInvalidCommand command, CancellationToken cancellationToken)
  {
    await _hubContext
      .Clients.Client(command.ConnectionId)
      .SendAsync(NotificationType.InvalidName, cancellationToken: cancellationToken);
  }
}
