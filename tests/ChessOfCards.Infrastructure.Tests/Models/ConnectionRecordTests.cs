using ChessOfCards.Infrastructure.Models;

namespace ChessOfCards.Infrastructure.Tests.Models;

public class ConnectionRecordTests
{
    [Fact]
    public void Constructor_WithConnectionId_CreatesRecordWithDefaultValues()
    {
        // Arrange
        var connectionId = "test-connection-123";
        var beforeCreation = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        // Act
        var record = new ConnectionRecord(connectionId);
        var afterCreation = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        // Assert
        Assert.NotNull(record);
        Assert.Equal(connectionId, record.ConnectionId);
        Assert.Null(record.GameCode);
        Assert.Null(record.PlayerRole);
        Assert.Null(record.PlayerName);
        Assert.InRange(record.ConnectedAt, beforeCreation, afterCreation);

        // TTL should be ~24 hours from now
        var expectedTtl = DateTimeOffset.UtcNow.AddHours(24).ToUnixTimeSeconds();
        Assert.InRange(record.Ttl, expectedTtl - 2, expectedTtl + 2); // Allow 2 second variance
    }

    [Fact]
    public void Constructor_WithConnectionIdAndPlayerName_SetsPlayerName()
    {
        // Arrange
        var connectionId = "test-connection-456";
        var playerName = "John Doe";

        // Act
        var record = new ConnectionRecord(connectionId, playerName);

        // Assert
        Assert.Equal(connectionId, record.ConnectionId);
        Assert.Equal(playerName, record.PlayerName);
        Assert.Null(record.GameCode);
        Assert.Null(record.PlayerRole);
    }

    [Fact]
    public void Constructor_WithNullPlayerName_CreatesRecordWithNullPlayerName()
    {
        // Arrange
        var connectionId = "test-connection-789";

        // Act
        var record = new ConnectionRecord(connectionId, null);

        // Assert
        Assert.Equal(connectionId, record.ConnectionId);
        Assert.Null(record.PlayerName);
    }

    [Fact]
    public void DefaultConstructor_CreatesEmptyRecord()
    {
        // Act
        var record = new ConnectionRecord();

        // Assert
        Assert.NotNull(record);
        Assert.Equal(string.Empty, record.ConnectionId);
        Assert.Null(record.GameCode);
        Assert.Null(record.PlayerRole);
        Assert.Null(record.PlayerName);
        Assert.Equal(0, record.ConnectedAt);
        Assert.Equal(0, record.Ttl);
    }

    [Fact]
    public void ConnectionRecord_CanSetGameCode()
    {
        // Arrange
        var record = new ConnectionRecord("conn-123");

        // Act
        record.GameCode = "GAME456";

        // Assert
        Assert.Equal("GAME456", record.GameCode);
    }

    [Fact]
    public void ConnectionRecord_CanSetPlayerRole()
    {
        // Arrange
        var record = new ConnectionRecord("conn-123");

        // Act
        record.PlayerRole = "HOST";

        // Assert
        Assert.Equal("HOST", record.PlayerRole);
    }

    [Theory]
    [InlineData("HOST")]
    [InlineData("GUEST")]
    public void ConnectionRecord_CanSetDifferentPlayerRoles(string role)
    {
        // Arrange
        var record = new ConnectionRecord("conn-123");

        // Act
        record.PlayerRole = role;

        // Assert
        Assert.Equal(role, record.PlayerRole);
    }

    [Fact]
    public void ConnectionRecord_CanUpdatePlayerName()
    {
        // Arrange
        var record = new ConnectionRecord("conn-123", "Initial Name");

        // Act
        record.PlayerName = "Updated Name";

        // Assert
        Assert.Equal("Updated Name", record.PlayerName);
    }

    [Fact]
    public void ConnectionRecord_WithGameAndRole_StoresAllProperties()
    {
        // Arrange
        var connectionId = "full-record-123";
        var playerName = "Alice";

        // Act
        var record = new ConnectionRecord(connectionId, playerName)
        {
            GameCode = "ABC123",
            PlayerRole = "HOST"
        };

        // Assert
        Assert.Equal(connectionId, record.ConnectionId);
        Assert.Equal(playerName, record.PlayerName);
        Assert.Equal("ABC123", record.GameCode);
        Assert.Equal("HOST", record.PlayerRole);
    }

    [Fact]
    public void ConnectionRecord_TtlIs24HoursFromCreation()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var expectedTtl = now.AddHours(24).ToUnixTimeSeconds();

        // Act
        var record = new ConnectionRecord("conn-ttl-test");

        // Assert
        // Allow 2 second variance due to execution time
        Assert.InRange(record.Ttl, expectedTtl - 2, expectedTtl + 2);

        // Verify it's approximately 24 hours in the future
        var ttlOffset = DateTimeOffset.FromUnixTimeSeconds(record.Ttl);
        var hoursDifference = (ttlOffset - now).TotalHours;
        Assert.InRange(hoursDifference, 23.99, 24.01);
    }

    [Fact]
    public void ConnectionRecord_ConnectedAtIsSetToCurrentTime()
    {
        // Arrange
        var before = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        // Act
        var record = new ConnectionRecord("conn-time-test");

        // Assert
        var after = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        Assert.InRange(record.ConnectedAt, before, after);
    }

    [Fact]
    public void ConnectionRecord_MultipleInstances_HaveDifferentTimestamps()
    {
        // Act
        var record1 = new ConnectionRecord("conn-1");
        System.Threading.Thread.Sleep(10); // Small delay
        var record2 = new ConnectionRecord("conn-2");

        // Assert
        Assert.NotEqual(record1.ConnectionId, record2.ConnectionId);
        // Second record should have equal or later timestamp
        Assert.True(record2.ConnectedAt >= record1.ConnectedAt);
    }

    [Fact]
    public void ConnectionRecord_CanClearGameCode()
    {
        // Arrange
        var record = new ConnectionRecord("conn-clear-test")
        {
            GameCode = "CLEAR123"
        };

        // Act
        record.GameCode = null;

        // Assert
        Assert.Null(record.GameCode);
    }

    [Fact]
    public void ConnectionRecord_CanClearPlayerRole()
    {
        // Arrange
        var record = new ConnectionRecord("conn-clear-role")
        {
            PlayerRole = "HOST"
        };

        // Act
        record.PlayerRole = null;

        // Assert
        Assert.Null(record.PlayerRole);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("special-chars-!@#$%")]
    [InlineData("very-long-connection-id-with-many-characters-1234567890")]
    public void ConnectionRecord_WithVariousConnectionIds_StoresCorrectly(string connectionId)
    {
        // Act
        var record = new ConnectionRecord(connectionId);

        // Assert
        Assert.Equal(connectionId, record.ConnectionId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Alice")]
    [InlineData("Bob the Builder")]
    [InlineData("Player123")]
    [InlineData("名前")] // Japanese characters
    public void ConnectionRecord_WithVariousPlayerNames_StoresCorrectly(string playerName)
    {
        // Act
        var record = new ConnectionRecord("conn-123", playerName);

        // Assert
        Assert.Equal(playerName, record.PlayerName);
    }
}
