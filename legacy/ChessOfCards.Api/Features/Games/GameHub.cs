using ChessOfCards.Application.Features.Games;
using MediatR;
using Microsoft.AspNetCore.SignalR;

namespace ChessOfCards.Api.Features.Games;

public class GameHub(IMediator mediator) : Hub
{
  private readonly IMediator _mediator = mediator;

  public async Task CreatePendingGame(CreatePendingGameRequest request)
  {
    await _mediator.Send(
      new CreatePendingGameCommand(Context.ConnectionId, request.DurationOption, request.HostName)
    );
  }

  public async Task JoinGame(JoinGameRequest request)
  {
    await _mediator.Send(
      new JoinGameCommand(Context.ConnectionId, request.GameCode, request.GuestName)
    );
  }

  public async Task PassMove()
  {
    await _mediator.Send(new PassMoveCommand(Context.ConnectionId));
  }

  public async Task MakeMove(MakeMoveRequest request)
  {
    await _mediator.Publish(
      new MakeMoveCommand(Context.ConnectionId, request.Move, request.RearrangedCardsInHand)
    );
  }

  public async Task ResignGame()
  {
    await _mediator.Publish(new ResignGameCommand(Context.ConnectionId));
  }

  public async Task DeletePendingGame()
  {
    await _mediator.Publish(new DeletePendingGameCommand(Context.ConnectionId));
  }

  public async Task RearrangeHand(RearrangeHandRequest request)
  {
    await _mediator.Publish(new RearrangeHandCommand(Context.ConnectionId, request.Cards));
  }

  public async Task OfferDraw()
  {
    await _mediator.Publish(new OfferDrawCommand(Context.ConnectionId));
  }

  public async Task AcceptDrawOffer()
  {
    await _mediator.Publish(new AcceptDrawOfferCommand(Context.ConnectionId));
  }

  public async Task SendChatMessage(SendChatMessageRequest request)
  {
    await _mediator.Publish(new SendChatMessageCommand(Context.ConnectionId, request.RawMessage));
  }

  public async Task MarkLatestReadChatMessage(MarkLatestReadChatMessageRequest request)
  {
    await _mediator.Publish(
      new MarkLatestReadChatMessageCommand(Context.ConnectionId, request.LatestIndex)
    );
  }

  public override async Task OnDisconnectedAsync(Exception? _)
  {
    await _mediator.Publish(new PlayerDisconnectedCommand(Context.ConnectionId));
  }
}
