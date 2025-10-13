using ChessOfCards.Application.Features.Games;
using ChessOfCards.Application.Models;
using MediatR;
using Microsoft.AspNetCore.SignalR;

namespace ChessOfCards.Api.Features.Games;

public class DrawOfferedCommandHandler(IHubContext<GameHub> hubContext)
  : INotificationHandler<DrawOfferedCommand>
{
  private readonly IHubContext<GameHub> _hubContext = hubContext;

  public async Task Handle(DrawOfferedCommand command, CancellationToken cancellationToken)
  {
    await _hubContext
      .Clients.Client(command.ConnectionId)
      .SendAsync(NotificationType.DrawOffered, cancellationToken: cancellationToken);
  }
}
