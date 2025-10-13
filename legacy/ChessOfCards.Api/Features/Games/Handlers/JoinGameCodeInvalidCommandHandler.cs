using ChessOfCards.Application.Features.Games;
using ChessOfCards.Application.Models;
using MediatR;
using Microsoft.AspNetCore.SignalR;

namespace ChessOfCards.Api.Features.Games;

public class JoinGameCodeInvalidCommandHandler(IHubContext<GameHub> hubContext)
  : INotificationHandler<JoinGameCodeInvalidCommand>
{
  private readonly IHubContext<GameHub> _hubContext = hubContext;

  public async Task Handle(JoinGameCodeInvalidCommand command, CancellationToken cancellationToken)
  {
    await _hubContext
      .Clients.Client(command.ConnectionId)
      .SendAsync(NotificationType.InvalidGameCode, cancellationToken: cancellationToken);
  }
}
