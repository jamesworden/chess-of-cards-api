using ChessOfCards.Infrastructure.Models;

namespace ChessOfCards.Infrastructure.Tests.Models;

public class GameTimerRecordTests
{
    [Fact]
    public void CreateGameClock_WithValidParameters_CreatesTimerWithCorrectProperties()
    {
        // Arrange
        var gameCode = "TEST123";
        var playerRole = "HOST";
        var totalSeconds = 300.0; // 5 minutes
        var beforeCreation = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        // Act
        var timer = GameTimerRecord.CreateGameClock(gameCode, playerRole, totalSeconds);
        var afterCreation = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        // Assert
        Assert.NotNull(timer);
        Assert.Equal($"GAME#{gameCode}#CLOCK#{playerRole}", timer.TimerId);
        Assert.Equal(gameCode, timer.GameCode);
        Assert.Equal($"GAME_CLOCK_{playerRole}", timer.TimerType);
        Assert.Equal(playerRole, timer.PlayerRole);
        Assert.Equal(0, timer.SecondsElapsed);
        Assert.Equal(totalSeconds, timer.SecondsRemaining);

        // Verify timestamps are within reasonable range
        Assert.InRange(timer.StartedAt, beforeCreation, afterCreation);
        Assert.InRange(timer.ExpiresAt, beforeCreation + (long)totalSeconds, afterCreation + (long)totalSeconds);
        Assert.InRange(timer.Ttl, beforeCreation + (long)totalSeconds + 3600, afterCreation + (long)totalSeconds + 3600);
    }

    [Fact]
    public void CreateGameClock_ForHostPlayer_CreatesHostClock()
    {
        // Arrange
        var gameCode = "ABC456";
        var playerRole = "HOST";
        var totalSeconds = 600.0;

        // Act
        var timer = GameTimerRecord.CreateGameClock(gameCode, playerRole, totalSeconds);

        // Assert
        Assert.Equal("GAME#ABC456#CLOCK#HOST", timer.TimerId);
        Assert.Equal("GAME_CLOCK_HOST", timer.TimerType);
        Assert.Equal("HOST", timer.PlayerRole);
    }

    [Fact]
    public void CreateGameClock_ForGuestPlayer_CreatesGuestClock()
    {
        // Arrange
        var gameCode = "XYZ789";
        var playerRole = "GUEST";
        var totalSeconds = 600.0;

        // Act
        var timer = GameTimerRecord.CreateGameClock(gameCode, playerRole, totalSeconds);

        // Assert
        Assert.Equal("GAME#XYZ789#CLOCK#GUEST", timer.TimerId);
        Assert.Equal("GAME_CLOCK_GUEST", timer.TimerType);
        Assert.Equal("GUEST", timer.PlayerRole);
    }

    [Fact]
    public void CreateGameClock_WithDifferentDurations_CreatesTimersWithCorrectExpiry()
    {
        // Arrange
        var gameCode = "DURATION_TEST";
        var playerRole = "HOST";

        // Act & Assert - 1 minute
        var oneMinute = GameTimerRecord.CreateGameClock(gameCode, playerRole, 60.0);
        Assert.Equal(60.0, oneMinute.SecondsRemaining);

        // Act & Assert - 10 minutes
        var tenMinutes = GameTimerRecord.CreateGameClock(gameCode, playerRole, 600.0);
        Assert.Equal(600.0, tenMinutes.SecondsRemaining);

        // Act & Assert - 1 hour
        var oneHour = GameTimerRecord.CreateGameClock(gameCode, playerRole, 3600.0);
        Assert.Equal(3600.0, oneHour.SecondsRemaining);
    }

    [Fact]
    public void CreateDisconnectTimer_WithValidParameters_CreatesTimerWithCorrectProperties()
    {
        // Arrange
        var gameCode = "DISC123";
        var playerRole = "GUEST";
        var gracePeriod = 30.0;
        var beforeCreation = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        // Act
        var timer = GameTimerRecord.CreateDisconnectTimer(gameCode, playerRole, gracePeriod);
        var afterCreation = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        // Assert
        Assert.NotNull(timer);
        Assert.Equal($"DISCONNECT#{gameCode}#{playerRole}", timer.TimerId);
        Assert.Equal(gameCode, timer.GameCode);
        Assert.Equal("DISCONNECT", timer.TimerType);
        Assert.Equal(playerRole, timer.PlayerRole);
        Assert.Equal(0, timer.SecondsElapsed);
        Assert.Equal(gracePeriod, timer.SecondsRemaining);

        // Verify timestamps
        Assert.InRange(timer.StartedAt, beforeCreation, afterCreation);
        Assert.InRange(timer.ExpiresAt, beforeCreation + (long)gracePeriod, afterCreation + (long)gracePeriod);
        Assert.InRange(timer.Ttl, beforeCreation + (long)gracePeriod + 600, afterCreation + (long)gracePeriod + 600);
    }

    [Fact]
    public void CreateDisconnectTimer_WithDefaultGracePeriod_Uses30Seconds()
    {
        // Arrange
        var gameCode = "DEFAULT_GRACE";
        var playerRole = "HOST";

        // Act
        var timer = GameTimerRecord.CreateDisconnectTimer(gameCode, playerRole);

        // Assert
        Assert.Equal(30.0, timer.SecondsRemaining);
    }

    [Fact]
    public void CreateDisconnectTimer_ForHost_CreatesHostDisconnectTimer()
    {
        // Arrange
        var gameCode = "HOST_DISC";
        var playerRole = "HOST";

        // Act
        var timer = GameTimerRecord.CreateDisconnectTimer(gameCode, playerRole, 45.0);

        // Assert
        Assert.Equal("DISCONNECT#HOST_DISC#HOST", timer.TimerId);
        Assert.Equal("DISCONNECT", timer.TimerType);
        Assert.Equal("HOST", timer.PlayerRole);
        Assert.Equal(45.0, timer.SecondsRemaining);
    }

    [Fact]
    public void CreateDisconnectTimer_ForGuest_CreatesGuestDisconnectTimer()
    {
        // Arrange
        var gameCode = "GUEST_DISC";
        var playerRole = "GUEST";

        // Act
        var timer = GameTimerRecord.CreateDisconnectTimer(gameCode, playerRole, 60.0);

        // Assert
        Assert.Equal("DISCONNECT#GUEST_DISC#GUEST", timer.TimerId);
        Assert.Equal("DISCONNECT", timer.TimerType);
        Assert.Equal("GUEST", timer.PlayerRole);
        Assert.Equal(60.0, timer.SecondsRemaining);
    }

    [Fact]
    public void CreateDisconnectTimer_WithCustomGracePeriod_UsesProvidedValue()
    {
        // Arrange
        var gameCode = "CUSTOM_GRACE";
        var playerRole = "HOST";
        var customGracePeriod = 120.0; // 2 minutes

        // Act
        var timer = GameTimerRecord.CreateDisconnectTimer(gameCode, playerRole, customGracePeriod);

        // Assert
        Assert.Equal(120.0, timer.SecondsRemaining);
    }

    [Fact]
    public void GameTimerRecord_DefaultConstructor_CreatesEmptyTimer()
    {
        // Act
        var timer = new GameTimerRecord();

        // Assert
        Assert.NotNull(timer);
        Assert.Equal(string.Empty, timer.TimerId);
        Assert.Equal(string.Empty, timer.GameCode);
        Assert.Equal(string.Empty, timer.TimerType);
        Assert.Null(timer.PlayerRole);
        Assert.Equal(0, timer.ExpiresAt);
        Assert.Equal(0, timer.StartedAt);
        Assert.Null(timer.PausedAt);
        Assert.Equal(0, timer.SecondsElapsed);
        Assert.Equal(0, timer.SecondsRemaining);
        Assert.Equal(0, timer.Ttl);
    }

    [Fact]
    public void GameTimerRecord_CanSetPausedAt()
    {
        // Arrange
        var timer = GameTimerRecord.CreateGameClock("TEST", "HOST", 300);
        var pauseTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        // Act
        timer.PausedAt = pauseTime;

        // Assert
        Assert.NotNull(timer.PausedAt);
        Assert.Equal(pauseTime, timer.PausedAt.Value);
    }

    [Fact]
    public void GameTimerRecord_CanUpdateSecondsElapsedAndRemaining()
    {
        // Arrange
        var timer = GameTimerRecord.CreateGameClock("TEST", "HOST", 300);

        // Act
        timer.SecondsElapsed = 50.5;
        timer.SecondsRemaining = 249.5;

        // Assert
        Assert.Equal(50.5, timer.SecondsElapsed);
        Assert.Equal(249.5, timer.SecondsRemaining);
    }

    [Fact]
    public void CreateGameClock_WithZeroSeconds_CreatesExpiredTimer()
    {
        // Arrange
        var gameCode = "INSTANT";
        var playerRole = "HOST";

        // Act
        var timer = GameTimerRecord.CreateGameClock(gameCode, playerRole, 0);

        // Assert
        Assert.Equal(0, timer.SecondsRemaining);
        Assert.True(timer.ExpiresAt <= DateTimeOffset.UtcNow.ToUnixTimeSeconds());
    }

    [Theory]
    [InlineData("HOST")]
    [InlineData("GUEST")]
    [InlineData("OBSERVER")]
    public void CreateGameClock_WithDifferentRoles_CreatesCorrectTimerIds(string role)
    {
        // Arrange
        var gameCode = "ROLE_TEST";

        // Act
        var timer = GameTimerRecord.CreateGameClock(gameCode, role, 300);

        // Assert
        Assert.Equal($"GAME#{gameCode}#CLOCK#{role}", timer.TimerId);
        Assert.Equal($"GAME_CLOCK_{role}", timer.TimerType);
        Assert.Equal(role, timer.PlayerRole);
    }

    [Theory]
    [InlineData("HOST")]
    [InlineData("GUEST")]
    public void CreateDisconnectTimer_WithDifferentRoles_CreatesCorrectTimerIds(string role)
    {
        // Arrange
        var gameCode = "ROLE_DISC_TEST";

        // Act
        var timer = GameTimerRecord.CreateDisconnectTimer(gameCode, role);

        // Assert
        Assert.Equal($"DISCONNECT#{gameCode}#{role}", timer.TimerId);
        Assert.Equal("DISCONNECT", timer.TimerType);
        Assert.Equal(role, timer.PlayerRole);
    }
}
