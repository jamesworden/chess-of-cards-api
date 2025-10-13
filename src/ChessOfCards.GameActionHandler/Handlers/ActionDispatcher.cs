using Amazon.Lambda.Core;
using ChessOfCards.GameActionHandler.Requests;
using ChessOfCards.GameActionHandler.Validators;
using ChessOfCards.Infrastructure.Messages;
using ChessOfCards.Infrastructure.Services;
using ChessOfCards.Shared.Utilities;
using MediatR;

namespace ChessOfCards.GameActionHandler.Handlers;

/// <summary>
/// Dispatches incoming actions to appropriate MediatR command handlers.
/// </summary>
public class ActionDispatcher
{
    private readonly IMediator _mediator;
    private readonly WebSocketService _webSocketService;

    public ActionDispatcher(IMediator mediator, WebSocketService webSocketService)
    {
        _mediator = mediator;
        _webSocketService = webSocketService;
    }

    public async Task DispatchAsync(
        string action,
        string connectionId,
        object? data,
        ILambdaContext context
    )
    {
        switch (action)
        {
            case "createPendingGame":
                await HandleCreatePendingGameAsync(connectionId, data);
                break;

            case "joinGame":
                await HandleJoinGameAsync(connectionId, data);
                break;

            case "deletePendingGame":
                await HandleDeletePendingGameAsync(connectionId);
                break;

            default:
                await HandleUnknownActionAsync(connectionId, action, context);
                break;
        }
    }

    private async Task HandleCreatePendingGameAsync(string connectionId, object? data)
    {
        var createRequest = JsonSerializationHelper.DeserializeData<CreatePendingGameRequest>(data);
        if (createRequest == null)
        {
            await SendErrorAsync(connectionId, "Invalid request data");
            return;
        }

        var validator = new CreatePendingGameRequestValidator();
        var validationResult = await validator.ValidateAsync(createRequest);

        if (!validationResult.IsValid)
        {
            var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
            await SendErrorAsync(connectionId, $"Validation failed: {errors}");
            return;
        }

        await _mediator.Send(createRequest.ToCommand(connectionId));
    }

    private async Task HandleJoinGameAsync(string connectionId, object? data)
    {
        var joinRequest = JsonSerializationHelper.DeserializeData<JoinGameRequest>(data);
        if (joinRequest == null)
        {
            await SendErrorAsync(connectionId, "Invalid request data");
            return;
        }

        var validator = new JoinGameRequestValidator();
        var validationResult = await validator.ValidateAsync(joinRequest);

        if (!validationResult.IsValid)
        {
            var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
            await SendErrorAsync(connectionId, $"Validation failed: {errors}");
            return;
        }

        await _mediator.Send(joinRequest.ToCommand(connectionId));
    }

    private async Task HandleDeletePendingGameAsync(string connectionId)
    {
        await _mediator.Send(
            new Application.Features.Games.Commands.DeletePendingGameCommand(connectionId)
        );
    }

    private async Task HandleUnknownActionAsync(
        string connectionId,
        string action,
        ILambdaContext context
    )
    {
        context.Logger.LogWarning($"Unknown action: {action}");
        await SendErrorAsync(connectionId, $"Unknown action: {action}");
    }

    private async Task SendErrorAsync(string connectionId, string message)
    {
        await _webSocketService.SendMessageAsync(
            connectionId,
            new WebSocketMessage(MessageTypes.Error, new { error = message })
        );
    }
}
