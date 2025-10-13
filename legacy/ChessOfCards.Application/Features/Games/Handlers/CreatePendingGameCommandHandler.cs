using ChessOfCards.DataAccess.Interfaces;
using ChessOfCards.Domain.Features.Games;
using ChessOfCards.Domain.Shared.Util;
using MediatR;

namespace ChessOfCards.Application.Features.Games;

public class CreatePendingGameCommandHandler(
  IPendingGameRepository pendingGameRepository,
  IMediator mediator
) : IRequestHandler<CreatePendingGameCommand>
{
  private readonly IPendingGameRepository _pendingGameRepository = pendingGameRepository;

  private readonly IMediator _mediator = mediator;

  private static readonly Random Random = new();

  private static readonly string Consonants = "BCDFGHJKLMNPQRSTVWXZ";

  public async Task Handle(CreatePendingGameCommand command, CancellationToken cancellationToken)
  {
    var name = string.IsNullOrWhiteSpace(command.HostName) ? null : command.HostName;
    if (name is not null && (name.ReplaceBadWordsWithAsterisks() != name))
    {
      await _mediator.Publish(new GameNameInvalidCommand(command.ConnectionId), cancellationToken);
      return;
    }

    string gameCode = GenerateUniqueGameCode();
    var pendingGame = new PendingGame(gameCode, command.ConnectionId, command.DurationOption, name);

    _pendingGameRepository.Add(pendingGame);

    await _mediator.Publish(
      new CreatedPendingGameCommand(command.ConnectionId, pendingGame.ToPendingGameView()),
      cancellationToken
    );
  }

  private string GenerateUniqueGameCode()
  {
    var numRetries = 10;
    var currentRetry = 0;

    while (currentRetry < numRetries)
    {
      var gameCode = GenerateRandomLetterString(4).ToUpper();
      var gameCodeIsUnused = _pendingGameRepository.FindByGameCode(gameCode) is null;

      if (gameCodeIsUnused)
      {
        return gameCode;
      }
      else
      {
        currentRetry++;
      }
    }

    throw new Exception("Unable to generate an unique game code.");
  }

  private static string GenerateRandomLetterString(int length)
  {
    // No bad words can be formed without vowels.
    var chars = Enumerable.Repeat(Consonants, length).Select(s => s[Random.Next(s.Length)]);
    var charArray = chars.ToArray();
    return new string(charArray);
  }
}
