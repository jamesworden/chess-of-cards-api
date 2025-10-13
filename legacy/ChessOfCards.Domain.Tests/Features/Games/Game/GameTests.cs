using ChessOfCards.Domain.Features.Games;

namespace ChessOfCards.Domain.Tests;

public class GameTests
{
  [Fact]
  public void Game_ShouldCreateCandidateMoveForPair_WhenPlayerHasPairInHand_AndWhenLaneHasCardInFirstPosition()
  {
    // Arrange
    var card1 = new Card(Kind.Nine, Suit.Hearts, PlayerOrNone.Host);

    var lane1 = new Lane(
      [
        [card1],
        [],
        [],
        [],
        [],
        [],
        []
      ]
    )
    {
      LaneAdvantage = PlayerOrNone.None,
      LastCardPlayed = card1,
      WonBy = PlayerOrNone.None
    };

    var lanes = new Lane[]
    {
      lane1,
      new(
        [
          [],
          [],
          [],
          [],
          [],
          [],
          []
        ]
      ),
      new(
        [
          [],
          [],
          [],
          [],
          [],
          [],
          []
        ]
      ),
      new(
        [
          [],
          [],
          [],
          [],
          [],
          [],
          []
        ]
      ),
      new(
        [
          [],
          [],
          [],
          [],
          [],
          [],
          []
        ]
      )
    };

    var game = new Game(
      "hostConnectionId",
      "guestConnectionId",
      "ABCD",
      DurationOption.FiveMinutes,
      "HostName",
      "GuestName"
    );

    game.SetLanes(lanes);

    game.SetHostHand(
      new Hand(
        [
          new Card(Kind.Nine, Suit.Clubs, PlayerOrNone.None),
          new Card(Kind.Nine, Suit.Spades, PlayerOrNone.None)
        ]
      )
    );

    // Act
    var candidateMoves = game.GetCandidateMoves(true, true);

    // Assert
    var relevantCandidateMoves = candidateMoves.Where(move =>
      move.IsValid
      && move.Move.PlaceCardAttempts.Count == 2
      && move.Move.PlaceCardAttempts[0].TargetLaneIndex == 0
      && move.Move.PlaceCardAttempts[0].TargetRowIndex == 1
    );
    Assert.True(relevantCandidateMoves.Any());
  }
}
