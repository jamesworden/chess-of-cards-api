using ChessOfCards.Application.Features.Games;
using ChessOfCards.Application.Models;
using MediatR;
using Microsoft.AspNetCore.SignalR;

namespace ChessOfCards.Api.Features.Games;

public class CreatedPendingGameCommandHandler(IHubContext<GameHub> hubContext)
  : INotificationHandler<CreatedPendingGameCommand>
{
  private readonly IHubContext<GameHub> _hubContext = hubContext;

  public async Task Handle(CreatedPendingGameCommand command, CancellationToken cancellationToken)
  {
    await _hubContext
      .Clients.Client(command.ConnectionId)
      .SendAsync(
        NotificationType.CreatedPendingGame,
        command.PendingGameView,
        cancellationToken: cancellationToken
      );
  }
}
