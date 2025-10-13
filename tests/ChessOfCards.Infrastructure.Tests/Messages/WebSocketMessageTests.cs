using System.Text.Json;
using ChessOfCards.Infrastructure.Messages;

namespace ChessOfCards.Infrastructure.Tests.Messages;

public class WebSocketMessageTests
{
    [Fact]
    public void WebSocketMessage_DefaultConstructor_CreatesEmptyMessage()
    {
        // Act
        var message = new WebSocketMessage();

        // Assert
        Assert.NotNull(message);
        Assert.Equal(string.Empty, message.Type);
        Assert.Null(message.Data);
    }

    [Fact]
    public void WebSocketMessage_WithTypeOnly_CreatesMessageWithType()
    {
        // Arrange
        var messageType = "TestMessage";

        // Act
        var message = new WebSocketMessage(messageType);

        // Assert
        Assert.Equal(messageType, message.Type);
        Assert.Null(message.Data);
    }

    [Fact]
    public void WebSocketMessage_WithTypeAndData_CreatesMessageWithBoth()
    {
        // Arrange
        var messageType = "GameStarted";
        var data = new { GameCode = "ABC123", Players = 2 };

        // Act
        var message = new WebSocketMessage(messageType, data);

        // Assert
        Assert.Equal(messageType, message.Type);
        Assert.NotNull(message.Data);
    }

    [Fact]
    public void WebSocketMessage_WithNullData_CreatesMessageWithNullData()
    {
        // Arrange
        var messageType = "Disconnected";

        // Act
        var message = new WebSocketMessage(messageType, null);

        // Assert
        Assert.Equal(messageType, message.Type);
        Assert.Null(message.Data);
    }

    [Fact]
    public void WebSocketMessage_SerializesToJson_WithCamelCase()
    {
        // Arrange
        var message = new WebSocketMessage("TestType", new { GameCode = "ABC123" });

        // Act
        var json = JsonSerializer.Serialize(message);

        // Assert
        Assert.Contains("\"type\"", json);
        Assert.Contains("\"data\"", json);
        Assert.Contains("TestType", json);
    }

    [Fact]
    public void WebSocketMessage_DeserializesFromJson_Correctly()
    {
        // Arrange
        var json = """
        {
            "type": "GameStarted",
            "data": {
                "gameCode": "XYZ789"
            }
        }
        """;

        // Act
        var message = JsonSerializer.Deserialize<WebSocketMessage>(json);

        // Assert
        Assert.NotNull(message);
        Assert.Equal("GameStarted", message.Type);
        Assert.NotNull(message.Data);
    }

    [Fact]
    public void WebSocketMessageGeneric_DefaultConstructor_CreatesEmptyMessage()
    {
        // Act
        var message = new WebSocketMessage<string>();

        // Assert
        Assert.NotNull(message);
        Assert.Equal(string.Empty, message.Type);
        Assert.Null(message.Data);
    }

    [Fact]
    public void WebSocketMessageGeneric_WithTypeAndData_CreatesTypedMessage()
    {
        // Arrange
        var messageType = "PlayerJoined";
        var data = new PlayerData { Name = "Alice", Role = "HOST" };

        // Act
        var message = new WebSocketMessage<PlayerData>(messageType, data);

        // Assert
        Assert.Equal(messageType, message.Type);
        Assert.NotNull(message.Data);
        Assert.Equal("Alice", message.Data.Name);
        Assert.Equal("HOST", message.Data.Role);
    }

    [Fact]
    public void WebSocketMessageGeneric_WithDefaultData_CreatesMessageWithDefault()
    {
        // Arrange
        var messageType = "EmptyMessage";

        // Act
        var message = new WebSocketMessage<string>(messageType);

        // Assert
        Assert.Equal(messageType, message.Type);
        Assert.Null(message.Data);
    }

    [Fact]
    public void WebSocketMessageGeneric_WithValueType_StoresValueCorrectly()
    {
        // Arrange
        var messageType = "CountUpdate";
        var count = 42;

        // Act
        var message = new WebSocketMessage<int>(messageType, count);

        // Assert
        Assert.Equal(messageType, message.Type);
        Assert.Equal(42, message.Data);
    }

    [Fact]
    public void WebSocketMessageGeneric_SerializesToJson_WithTypedData()
    {
        // Arrange
        var data = new GameData { GameCode = "TEST123", PlayerCount = 2 };
        var message = new WebSocketMessage<GameData>("GameInfo", data);

        // Act
        var json = JsonSerializer.Serialize(message);

        // Assert
        Assert.Contains("\"type\"", json);
        Assert.Contains("\"data\"", json);
        Assert.Contains("GameInfo", json);
        Assert.Contains("TEST123", json);
    }

    [Fact]
    public void WebSocketMessageGeneric_DeserializesFromJson_WithTypedData()
    {
        // Arrange
        var json = """
        {
            "type": "GameUpdate",
            "data": {
                "gameCode": "ABC123",
                "playerCount": 2
            }
        }
        """;

        // Act
        var message = JsonSerializer.Deserialize<WebSocketMessage<GameData>>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        // Assert
        Assert.NotNull(message);
        Assert.Equal("GameUpdate", message.Type);
        Assert.NotNull(message.Data);
        Assert.Equal("ABC123", message.Data.GameCode);
        Assert.Equal(2, message.Data.PlayerCount);
    }

    [Fact]
    public void ActionRequest_DefaultConstructor_CreatesEmptyRequest()
    {
        // Act
        var request = new ActionRequest();

        // Assert
        Assert.NotNull(request);
        Assert.Equal(string.Empty, request.Action);
        Assert.Null(request.Data);
    }

    [Fact]
    public void ActionRequest_SerializesToJson_WithCamelCase()
    {
        // Arrange
        var request = new ActionRequest
        {
            Action = "joinGame",
            Data = new { GameCode = "ABC123", PlayerName = "Bob" }
        };

        // Act
        var json = JsonSerializer.Serialize(request);

        // Assert
        Assert.Contains("\"action\"", json);
        Assert.Contains("\"data\"", json);
        Assert.Contains("joinGame", json);
    }

    [Fact]
    public void ActionRequest_DeserializesFromJson_Correctly()
    {
        // Arrange
        var json = """
        {
            "action": "createGame",
            "data": {
                "playerName": "Alice"
            }
        }
        """;

        // Act
        var request = JsonSerializer.Deserialize<ActionRequest>(json);

        // Assert
        Assert.NotNull(request);
        Assert.Equal("createGame", request.Action);
        Assert.NotNull(request.Data);
    }

    [Fact]
    public void ActionRequestGeneric_WithTypedData_StoresDataCorrectly()
    {
        // Arrange
        var data = new JoinGameData { GameCode = "XYZ789", PlayerName = "Charlie" };

        // Act
        var request = new ActionRequest<JoinGameData>
        {
            Action = "joinGame",
            Data = data
        };

        // Assert
        Assert.Equal("joinGame", request.Action);
        Assert.NotNull(request.Data);
        Assert.Equal("XYZ789", request.Data.GameCode);
        Assert.Equal("Charlie", request.Data.PlayerName);
    }

    [Fact]
    public void ActionRequestGeneric_DeserializesFromJson_WithTypedData()
    {
        // Arrange
        var json = """
        {
            "action": "joinGame",
            "data": {
                "gameCode": "TEST456",
                "playerName": "Dave"
            }
        }
        """;

        // Act
        var request = JsonSerializer.Deserialize<ActionRequest<JoinGameData>>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        // Assert
        Assert.NotNull(request);
        Assert.Equal("joinGame", request.Action);
        Assert.NotNull(request.Data);
        Assert.Equal("TEST456", request.Data.GameCode);
        Assert.Equal("Dave", request.Data.PlayerName);
    }

    [Theory]
    [InlineData(MessageTypes.CreatedPendingGame, "CreatedPendingGame")]
    [InlineData(MessageTypes.GameStarted, "GameStarted")]
    [InlineData(MessageTypes.GameUpdated, "GameUpdated")]
    [InlineData(MessageTypes.GameOver, "GameOver")]
    [InlineData(MessageTypes.OpponentDisconnected, "OpponentDisconnected")]
    [InlineData(MessageTypes.OpponentReconnected, "OpponentReconnected")]
    [InlineData(MessageTypes.PlayerReconnected, "PlayerReconnected")]
    [InlineData(MessageTypes.ChatMessageSent, "ChatMessageSent")]
    [InlineData(MessageTypes.DrawOffered, "DrawOffered")]
    [InlineData(MessageTypes.TurnSkipped, "TurnSkipped")]
    [InlineData(MessageTypes.JoinGameCodeInvalid, "JoinGameCodeInvalid")]
    [InlineData(MessageTypes.GameNameInvalid, "GameNameInvalid")]
    [InlineData(MessageTypes.LatestReadChatMessageMarked, "LatestReadChatMessageMarked")]
    [InlineData(MessageTypes.Error, "Error")]
    [InlineData(MessageTypes.Connected, "Connected")]
    public void MessageTypes_Constants_HaveCorrectValues(string constant, string expectedValue)
    {
        // Assert
        Assert.Equal(expectedValue, constant);
    }

    [Fact]
    public void MessageTypes_AllConstantsAreDefined()
    {
        // Assert - verify all expected message types exist
        Assert.NotNull(MessageTypes.CreatedPendingGame);
        Assert.NotNull(MessageTypes.GameStarted);
        Assert.NotNull(MessageTypes.GameUpdated);
        Assert.NotNull(MessageTypes.GameOver);
        Assert.NotNull(MessageTypes.OpponentDisconnected);
        Assert.NotNull(MessageTypes.OpponentReconnected);
        Assert.NotNull(MessageTypes.PlayerReconnected);
        Assert.NotNull(MessageTypes.ChatMessageSent);
        Assert.NotNull(MessageTypes.DrawOffered);
        Assert.NotNull(MessageTypes.TurnSkipped);
        Assert.NotNull(MessageTypes.JoinGameCodeInvalid);
        Assert.NotNull(MessageTypes.GameNameInvalid);
        Assert.NotNull(MessageTypes.LatestReadChatMessageMarked);
        Assert.NotNull(MessageTypes.Error);
        Assert.NotNull(MessageTypes.Connected);
    }

    [Fact]
    public void WebSocketMessage_WithMessageTypeConstant_UsesCorrectValue()
    {
        // Arrange & Act
        var message = new WebSocketMessage(MessageTypes.GameStarted, new { GameCode = "TEST" });

        // Assert
        Assert.Equal("GameStarted", message.Type);
    }
}

// Test data classes
public class PlayerData
{
    public string Name { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

public class GameData
{
    public string GameCode { get; set; } = string.Empty;
    public int PlayerCount { get; set; }
}

public class JoinGameData
{
    public string GameCode { get; set; } = string.Empty;
    public string PlayerName { get; set; } = string.Empty;
}
