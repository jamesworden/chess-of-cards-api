using System.Text;
using System.Text.Json;
using ChessOfCards.Infrastructure.Serialization;

namespace ChessOfCards.Infrastructure.Tests.Serialization;

public class CamelCaseLambdaJsonSerializerTests
{
    private readonly CamelCaseLambdaJsonSerializer _serializer;

    public CamelCaseLambdaJsonSerializerTests()
    {
        _serializer = new CamelCaseLambdaJsonSerializer();
    }

    [Fact]
    public void Serialize_WithPascalCaseProperties_SerializesToCamelCase()
    {
        // Arrange
        var data = new TestData
        {
            GameCode = "ABC123",
            PlayerName = "Alice",
            TotalScore = 100
        };

        using var stream = new MemoryStream();

        // Act
        _serializer.Serialize(data, stream);

        // Assert
        stream.Position = 0;
        var json = Encoding.UTF8.GetString(stream.ToArray());
        Assert.Contains("\"gameCode\"", json);
        Assert.Contains("\"playerName\"", json);
        Assert.Contains("\"totalScore\"", json);
        Assert.DoesNotContain("\"GameCode\"", json);
        Assert.DoesNotContain("\"PlayerName\"", json);
        Assert.DoesNotContain("\"TotalScore\"", json);
    }

    [Fact]
    public void Deserialize_WithCamelCaseJson_DeserializesToPascalCase()
    {
        // Arrange
        var json = """
        {
            "gameCode": "XYZ789",
            "playerName": "Bob",
            "totalScore": 250
        }
        """;
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

        // Act
        var result = _serializer.Deserialize<TestData>(stream);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("XYZ789", result.GameCode);
        Assert.Equal("Bob", result.PlayerName);
        Assert.Equal(250, result.TotalScore);
    }

    [Fact]
    public void Deserialize_WithPascalCaseJson_DeserializesToPascalCase()
    {
        // Arrange - serializer is case-insensitive
        var json = """
        {
            "GameCode": "TEST123",
            "PlayerName": "Charlie",
            "TotalScore": 500
        }
        """;
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

        // Act
        var result = _serializer.Deserialize<TestData>(stream);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("TEST123", result.GameCode);
        Assert.Equal("Charlie", result.PlayerName);
        Assert.Equal(500, result.TotalScore);
    }

    [Fact]
    public void Serialize_WithNullProperties_IgnoresNullValues()
    {
        // Arrange
        var data = new TestData
        {
            GameCode = "ABC123",
            PlayerName = null, // This should be omitted
            TotalScore = 100
        };

        using var stream = new MemoryStream();

        // Act
        _serializer.Serialize(data, stream);

        // Assert
        stream.Position = 0;
        var json = Encoding.UTF8.GetString(stream.ToArray());
        Assert.Contains("\"gameCode\"", json);
        Assert.Contains("\"totalScore\"", json);
        Assert.DoesNotContain("\"playerName\"", json);
    }

    [Fact]
    public void Serialize_WithNestedObject_SerializesNestedPropertiesToCamelCase()
    {
        // Arrange
        var data = new TestGameState
        {
            GameCode = "NESTED123",
            HostPlayer = new TestPlayer
            {
                PlayerName = "Alice",
                PlayerScore = 100
            },
            GuestPlayer = new TestPlayer
            {
                PlayerName = "Bob",
                PlayerScore = 150
            }
        };

        using var stream = new MemoryStream();

        // Act
        _serializer.Serialize(data, stream);

        // Assert
        stream.Position = 0;
        var json = Encoding.UTF8.GetString(stream.ToArray());
        Assert.Contains("\"gameCode\"", json);
        Assert.Contains("\"hostPlayer\"", json);
        Assert.Contains("\"guestPlayer\"", json);
        Assert.Contains("\"playerName\"", json);
        Assert.Contains("\"playerScore\"", json);
    }

    [Fact]
    public void Deserialize_WithNestedCamelCaseJson_DeserializesCorrectly()
    {
        // Arrange
        var json = """
        {
            "gameCode": "NESTED456",
            "hostPlayer": {
                "playerName": "Dave",
                "playerScore": 200
            },
            "guestPlayer": {
                "playerName": "Eve",
                "playerScore": 300
            }
        }
        """;
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

        // Act
        var result = _serializer.Deserialize<TestGameState>(stream);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("NESTED456", result.GameCode);
        Assert.NotNull(result.HostPlayer);
        Assert.Equal("Dave", result.HostPlayer.PlayerName);
        Assert.Equal(200, result.HostPlayer.PlayerScore);
        Assert.NotNull(result.GuestPlayer);
        Assert.Equal("Eve", result.GuestPlayer.PlayerName);
        Assert.Equal(300, result.GuestPlayer.PlayerScore);
    }

    [Fact]
    public void Serialize_WithList_SerializesToCamelCaseArray()
    {
        // Arrange
        var data = new TestCollection
        {
            GameCode = "LIST123",
            Players = new List<TestPlayer>
            {
                new TestPlayer { PlayerName = "Alice", PlayerScore = 100 },
                new TestPlayer { PlayerName = "Bob", PlayerScore = 200 }
            }
        };

        using var stream = new MemoryStream();

        // Act
        _serializer.Serialize(data, stream);

        // Assert
        stream.Position = 0;
        var json = Encoding.UTF8.GetString(stream.ToArray());
        Assert.Contains("\"gameCode\"", json);
        Assert.Contains("\"players\"", json);
        Assert.Contains("\"playerName\"", json);
        Assert.Contains("\"playerScore\"", json);
    }

    [Fact]
    public void Deserialize_WithArray_DeserializesCorrectly()
    {
        // Arrange
        var json = """
        {
            "gameCode": "ARRAY789",
            "players": [
                { "playerName": "Player1", "playerScore": 50 },
                { "playerName": "Player2", "playerScore": 75 }
            ]
        }
        """;
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

        // Act
        var result = _serializer.Deserialize<TestCollection>(stream);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("ARRAY789", result.GameCode);
        Assert.NotNull(result.Players);
        Assert.Equal(2, result.Players.Count);
        Assert.Equal("Player1", result.Players[0].PlayerName);
        Assert.Equal(50, result.Players[0].PlayerScore);
    }

    [Fact]
    public void Deserialize_WithMixedCaseJson_HandlesCaseInsensitively()
    {
        // Arrange - mixed case property names
        var json = """
        {
            "GameCode": "MIXED123",
            "playerName": "TestPlayer",
            "TotalScore": 999
        }
        """;
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

        // Act
        var result = _serializer.Deserialize<TestData>(stream);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("MIXED123", result.GameCode);
        Assert.Equal("TestPlayer", result.PlayerName);
        Assert.Equal(999, result.TotalScore);
    }

    [Fact]
    public void Serialize_WithEmptyObject_SerializesEmptyJson()
    {
        // Arrange
        var data = new TestData();

        using var stream = new MemoryStream();

        // Act
        _serializer.Serialize(data, stream);

        // Assert
        stream.Position = 0;
        var json = Encoding.UTF8.GetString(stream.ToArray());
        var deserialized = JsonSerializer.Deserialize<JsonElement>(json);
        Assert.Equal(JsonValueKind.Object, deserialized.ValueKind);
    }

    [Fact]
    public void Serialize_WithPrimitiveTypes_SerializesCorrectly()
    {
        // Arrange
        var data = new TestPrimitives
        {
            IntValue = 42,
            StringValue = "test",
            BoolValue = true,
            DoubleValue = 3.14159,
            LongValue = 1234567890L
        };

        using var stream = new MemoryStream();

        // Act
        _serializer.Serialize(data, stream);

        // Assert
        stream.Position = 0;
        var json = Encoding.UTF8.GetString(stream.ToArray());
        Assert.Contains("\"intValue\"", json);
        Assert.Contains("\"stringValue\"", json);
        Assert.Contains("\"boolValue\"", json);
        Assert.Contains("\"doubleValue\"", json);
        Assert.Contains("\"longValue\"", json);
        Assert.Contains("42", json);
        Assert.Contains("test", json);
        Assert.Contains("true", json);
        Assert.Contains("3.14159", json);
    }

    [Fact]
    public void Deserialize_WithPrimitiveTypes_DeserializesCorrectly()
    {
        // Arrange
        var json = """
        {
            "intValue": 99,
            "stringValue": "hello",
            "boolValue": false,
            "doubleValue": 2.71828,
            "longValue": 9876543210
        }
        """;
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

        // Act
        var result = _serializer.Deserialize<TestPrimitives>(stream);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(99, result.IntValue);
        Assert.Equal("hello", result.StringValue);
        Assert.False(result.BoolValue);
        Assert.Equal(2.71828, result.DoubleValue);
        Assert.Equal(9876543210L, result.LongValue);
    }

    [Fact]
    public void Serializer_CanRoundTrip_ComplexObject()
    {
        // Arrange
        var original = new TestGameState
        {
            GameCode = "ROUNDTRIP",
            HostPlayer = new TestPlayer { PlayerName = "Host", PlayerScore = 111 },
            GuestPlayer = new TestPlayer { PlayerName = "Guest", PlayerScore = 222 }
        };

        using var serializeStream = new MemoryStream();

        // Act - Serialize
        _serializer.Serialize(original, serializeStream);

        // Act - Deserialize
        serializeStream.Position = 0;
        var result = _serializer.Deserialize<TestGameState>(serializeStream);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(original.GameCode, result.GameCode);
        Assert.NotNull(result.HostPlayer);
        Assert.Equal(original.HostPlayer.PlayerName, result.HostPlayer.PlayerName);
        Assert.Equal(original.HostPlayer.PlayerScore, result.HostPlayer.PlayerScore);
        Assert.NotNull(result.GuestPlayer);
        Assert.Equal(original.GuestPlayer.PlayerName, result.GuestPlayer.PlayerName);
        Assert.Equal(original.GuestPlayer.PlayerScore, result.GuestPlayer.PlayerScore);
    }
}

// Test model classes
public class TestData
{
    public string? GameCode { get; set; }
    public string? PlayerName { get; set; }
    public int TotalScore { get; set; }
}

public class TestPlayer
{
    public string? PlayerName { get; set; }
    public int PlayerScore { get; set; }
}

public class TestGameState
{
    public string? GameCode { get; set; }
    public TestPlayer? HostPlayer { get; set; }
    public TestPlayer? GuestPlayer { get; set; }
}

public class TestCollection
{
    public string? GameCode { get; set; }
    public List<TestPlayer>? Players { get; set; }
}

public class TestPrimitives
{
    public int IntValue { get; set; }
    public string? StringValue { get; set; }
    public bool BoolValue { get; set; }
    public double DoubleValue { get; set; }
    public long LongValue { get; set; }
}
