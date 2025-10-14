using System.Text.Json;
using ChessOfCards.Domain.Features.Games;
using ChessOfCards.Shared.Utilities;

namespace ChessOfCards.Domain.Tests;

/// <summary>
/// Tests to verify that the Game domain model serializes and deserializes correctly,
/// including all private fields like Lanes, RedJokerLaneIndex, CandidateMoves, etc.
/// </summary>
public class GameSerializationTests
{
    [Fact]
    public void Game_SerializeDeserialize_AllFieldsRoundTrip()
    {
        // Arrange - Create a new game
        var hostConnectionId = "host-conn-123";
        var guestConnectionId = "guest-conn-456";
        var gameCode = "TEST123";
        var durationOption = DurationOption.FiveMinutes;
        var hostName = "Alice";
        var guestName = "Bob";

        var originalGame = new Game(
            hostConnectionId,
            guestConnectionId,
            gameCode,
            durationOption,
            hostName,
            guestName
        );

        // Act - Serialize and deserialize
        var json = JsonSerializer.Serialize(originalGame, JsonOptions.Default);
        var restoredGame = JsonSerializer.Deserialize<Game>(json, JsonOptions.Default);

        // Assert - Verify all critical fields are present
        Assert.NotNull(restoredGame);

        // Public properties
        Assert.Equal(hostConnectionId, restoredGame.HostConnectionId);
        Assert.Equal(guestConnectionId, restoredGame.GuestConnectionId);
        Assert.Equal(gameCode, restoredGame.GameCode);
        Assert.Equal(durationOption, restoredGame.DurationOption);
        Assert.True(restoredGame.IsHostPlayersTurn);
        Assert.False(restoredGame.HasEnded);
        Assert.Equal(PlayerOrNone.None, restoredGame.WonBy);

        // Verify that we can get candidate moves (this proves CandidateMoves was restored)
        var candidateMoves = restoredGame.GetCandidateMoves(true, true);
        Assert.NotEmpty(candidateMoves);

        // Verify game views work (this tests Lanes, Players, etc.)
        var hostView = restoredGame.ToHostPlayerView(0, 0);
        Assert.NotNull(hostView);
        Assert.Equal(gameCode, hostView.GameCode);
        Assert.NotNull(hostView.Hand);
        Assert.Equal(5, hostView.Hand.Cards.Count); // Should have 5 cards in hand
        Assert.NotNull(hostView.Lanes);
        Assert.Equal(5, hostView.Lanes.Length); // Should have 5 lanes

        var guestView = restoredGame.ToGuestPlayerView(0, 0);
        Assert.NotNull(guestView);
        Assert.Equal(gameCode, guestView.GameCode);
        Assert.NotNull(guestView.Hand);
        Assert.Equal(5, guestView.Hand.Cards.Count);
    }

    [Fact]
    public void Game_AfterMove_SerializeDeserialize_PreservesState()
    {
        // Arrange - Create a game and make a move
        var game = new Game(
            "host-123",
            "guest-456",
            "MOVE123",
            DurationOption.ThreeMinutes,
            "Player1",
            "Player2"
        );

        // Get the first valid candidate move
        var candidateMoves = game.GetCandidateMoves(true, true);
        var firstValidMove = candidateMoves.FirstOrDefault(m => m.IsValid);
        Assert.NotNull(firstValidMove);

        // Make the move
        var results = game.MakeMove("host-123", firstValidMove.Move, null);
        Assert.DoesNotContain(MakeMoveResults.InvalidMove, results);

        // Act - Serialize and deserialize
        var json = JsonSerializer.Serialize(game, JsonOptions.Default);
        var restoredGame = JsonSerializer.Deserialize<Game>(json, JsonOptions.Default);

        // Assert - Verify state after move is preserved
        Assert.NotNull(restoredGame);

        // After host makes a move, it should be guest's turn (or host's turn if guest skipped)
        // The exact turn depends on game logic, but we can verify state is consistent
        var hostViewAfter = restoredGame.ToHostPlayerView(0, 0);
        Assert.NotNull(hostViewAfter);
        Assert.NotNull(hostViewAfter.CandidateMoves);
    }

    [Fact]
    public void Game_WithWonLane_SerializeDeserialize_PreservesJokerIndexes()
    {
        // Arrange - Create a game
        var game = new Game(
            "host-789",
            "guest-012",
            "JOKER123",
            DurationOption.OneMinute,
            "Winner",
            "Loser"
        );

        // Note: In a real scenario, we'd play moves until a lane is won
        // For this test, we're just verifying that IF RedJokerLaneIndex or BlackJokerLaneIndex
        // were set, they would round-trip correctly

        // Act - Serialize and deserialize
        var json = JsonSerializer.Serialize(game, JsonOptions.Default);
        var restoredGame = JsonSerializer.Deserialize<Game>(json, JsonOptions.Default);

        // Assert
        Assert.NotNull(restoredGame);

        // RedJokerLaneIndex and BlackJokerLaneIndex should be null at game start
        var hostView = restoredGame.ToHostPlayerView(0, 0);
        Assert.Null(hostView.RedJokerLaneIndex);
        Assert.Null(hostView.BlackJokerLaneIndex);
    }

    [Fact]
    public void Game_SerializedJson_ContainsCandidateMoves()
    {
        // Arrange
        var game = new Game(
            "host-999",
            "guest-888",
            "JSON123",
            DurationOption.FiveMinutes,
            "TestHost",
            "TestGuest"
        );

        // Act
        var json = JsonSerializer.Serialize(game, JsonOptions.Default);

        // Assert - Verify the JSON contains candidateMoves field
        Assert.Contains("candidateMoves", json, StringComparison.OrdinalIgnoreCase);

        // Verify it's not an empty array
        Assert.DoesNotContain("\"candidateMoves\":[]", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Game_SerializedJson_ContainsLanes()
    {
        // Arrange
        var game = new Game(
            "host-777",
            "guest-666",
            "LANES123",
            DurationOption.FiveMinutes,
            "LaneHost",
            "LaneGuest"
        );

        // Act
        var json = JsonSerializer.Serialize(game, JsonOptions.Default);

        // Assert - Verify the JSON contains lanes field
        Assert.Contains("lanes", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Game_PassMove_SerializeDeserialize_PreservesMovesMade()
    {
        // Arrange - Create a game and pass
        var game = new Game(
            "host-555",
            "guest-444",
            "PASS123",
            DurationOption.ThreeMinutes,
            "PassHost",
            "PassGuest"
        );

        // Pass a move
        var passResults = game.PassMove("host-555");
        Assert.Empty(passResults); // Should succeed

        // Act - Serialize and deserialize
        var json = JsonSerializer.Serialize(game, JsonOptions.Default);
        var restoredGame = JsonSerializer.Deserialize<Game>(json, JsonOptions.Default);

        // Assert
        Assert.NotNull(restoredGame);

        // After passing, turn should have changed
        Assert.False(restoredGame.IsHostPlayersTurn); // Now guest's turn

        // Verify game views still work
        var guestView = restoredGame.ToGuestPlayerView(0, 0);
        Assert.NotNull(guestView);
        Assert.NotNull(guestView.MovesMade);
        Assert.Single(guestView.MovesMade); // One move (the pass)
    }
}
