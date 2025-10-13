using System.Text.Json;
using ChessOfCards.Shared.Utilities;

namespace ChessOfCards.Shared.Utilities.Tests;

public class JsonSerializationHelperTests
{
    [Fact]
    public void DeserializeData_WithValidObject_ReturnsTypedObject()
    {
        // Arrange
        var sourceData = new
        {
            Name = "John Doe",
            Age = 30,
            Email = "john@example.com"
        };

        // Act
        var result = JsonSerializationHelper.DeserializeData<TestPerson>(sourceData);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("John Doe", result.Name);
        Assert.Equal(30, result.Age);
        Assert.Equal("john@example.com", result.Email);
    }

    [Fact]
    public void DeserializeData_WithNull_ReturnsDefault()
    {
        // Arrange
        object? sourceData = null;

        // Act
        var result = JsonSerializationHelper.DeserializeData<TestPerson>(sourceData);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void DeserializeData_WithNullableType_ReturnsNull()
    {
        // Arrange
        object? sourceData = null;

        // Act
        var result = JsonSerializationHelper.DeserializeData<string?>(sourceData);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void DeserializeData_WithNestedObject_ReturnsTypedNestedObject()
    {
        // Arrange
        var sourceData = new
        {
            PersonName = "Jane Smith",
            Address = new
            {
                Street = "123 Main St",
                City = "Springfield",
                ZipCode = "12345"
            }
        };

        // Act
        var result = JsonSerializationHelper.DeserializeData<TestPersonWithAddress>(sourceData);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Jane Smith", result.PersonName);
        Assert.NotNull(result.Address);
        Assert.Equal("123 Main St", result.Address.Street);
        Assert.Equal("Springfield", result.Address.City);
        Assert.Equal("12345", result.Address.ZipCode);
    }

    [Fact]
    public void DeserializeData_WithList_ReturnsTypedList()
    {
        // Arrange
        var sourceData = new[]
        {
            new { Name = "Alice", Age = 25 },
            new { Name = "Bob", Age = 30 },
            new { Name = "Charlie", Age = 35 }
        };

        // Act
        var result = JsonSerializationHelper.DeserializeData<List<TestPerson>>(sourceData);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        Assert.Equal("Alice", result[0].Name);
        Assert.Equal(25, result[0].Age);
        Assert.Equal("Bob", result[1].Name);
        Assert.Equal(30, result[1].Age);
        Assert.Equal("Charlie", result[2].Name);
        Assert.Equal(35, result[2].Age);
    }

    [Fact]
    public void DeserializeData_WithDictionary_ReturnsTypedDictionary()
    {
        // Arrange
        var sourceData = new Dictionary<string, object>
        {
            { "player1", "Alice" },
            { "player2", "Bob" },
            { "player3", "Charlie" }
        };

        // Act
        var result = JsonSerializationHelper.DeserializeData<Dictionary<string, string>>(sourceData);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        Assert.Equal("Alice", result["player1"]);
        Assert.Equal("Bob", result["player2"]);
        Assert.Equal("Charlie", result["player3"]);
    }

    [Fact]
    public void DeserializeData_WithPrimitiveTypes_ReturnsTypedValues()
    {
        // Arrange - int
        var intData = 42;

        // Act
        var intResult = JsonSerializationHelper.DeserializeData<int>(intData);

        // Assert
        Assert.Equal(42, intResult);

        // Arrange - string
        var stringData = "test string";

        // Act
        var stringResult = JsonSerializationHelper.DeserializeData<string>(stringData);

        // Assert
        Assert.Equal("test string", stringResult);

        // Arrange - bool
        var boolData = true;

        // Act
        var boolResult = JsonSerializationHelper.DeserializeData<bool>(boolData);

        // Assert
        Assert.True(boolResult);

        // Arrange - double
        var doubleData = 3.14159;

        // Act
        var doubleResult = JsonSerializationHelper.DeserializeData<double>(doubleData);

        // Assert
        Assert.Equal(3.14159, doubleResult);
    }

    [Fact]
    public void DeserializeData_WithJsonElement_ReturnsTypedObject()
    {
        // Arrange
        var json = """
        {
            "Name": "Test User",
            "Age": 40,
            "Email": "test@example.com"
        }
        """;
        var jsonElement = JsonSerializer.Deserialize<JsonElement>(json);

        // Act
        var result = JsonSerializationHelper.DeserializeData<TestPerson>(jsonElement);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test User", result.Name);
        Assert.Equal(40, result.Age);
        Assert.Equal("test@example.com", result.Email);
    }

    [Fact]
    public void DeserializeData_WithMismatchedProperties_ReturnsPartialObject()
    {
        // Arrange
        var sourceData = new
        {
            Name = "Partial User",
            ExtraField = "This field doesn't exist in TestPerson"
        };

        // Act
        var result = JsonSerializationHelper.DeserializeData<TestPerson>(sourceData);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Partial User", result.Name);
        Assert.Equal(0, result.Age); // Default value for int
        Assert.Null(result.Email); // Default value for nullable string
    }

    [Fact]
    public void DeserializeData_WithCaseMismatch_ReturnsDefaultValues()
    {
        // Arrange - different casing (JsonSerializer is case-sensitive by default)
        var sourceData = new
        {
            name = "Case Test",
            age = 28,
            EMAIL = "case@test.com"
        };

        // Act
        var result = JsonSerializationHelper.DeserializeData<TestPerson>(sourceData);

        // Assert
        Assert.NotNull(result);
        // JsonSerializer is case-sensitive, so mismatched properties won't be set
        Assert.Null(result.Name);
        Assert.Equal(0, result.Age);
        Assert.Null(result.Email);
    }

    [Fact]
    public void DeserializeData_WithComplexGameData_ReturnsTypedObject()
    {
        // Arrange - simulating game data structure
        var sourceData = new
        {
            GameCode = "ABC123",
            HostConnectionId = "host-conn-123",
            GuestConnectionId = "guest-conn-456",
            CurrentTurn = "HOST",
            MoveCount = 5
        };

        // Act
        var result = JsonSerializationHelper.DeserializeData<TestGameData>(sourceData);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("ABC123", result.GameCode);
        Assert.Equal("host-conn-123", result.HostConnectionId);
        Assert.Equal("guest-conn-456", result.GuestConnectionId);
        Assert.Equal("HOST", result.CurrentTurn);
        Assert.Equal(5, result.MoveCount);
    }

    [Fact]
    public void DeserializeData_WithDateTimeOffset_ReturnsTypedObject()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var sourceData = new
        {
            EventName = "Game Started",
            Timestamp = now
        };

        // Act
        var result = JsonSerializationHelper.DeserializeData<TestEvent>(sourceData);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Game Started", result.EventName);
        // DateTimeOffset might lose some precision in serialization, so check within a small range
        Assert.True(Math.Abs((result.Timestamp - now).TotalMilliseconds) < 10);
    }
}

// Test model classes
public class TestPerson
{
    public string? Name { get; set; }
    public int Age { get; set; }
    public string? Email { get; set; }
}

public class TestAddress
{
    public string? Street { get; set; }
    public string? City { get; set; }
    public string? ZipCode { get; set; }
}

public class TestPersonWithAddress
{
    public string? PersonName { get; set; }
    public TestAddress? Address { get; set; }
}

public class TestGameData
{
    public string? GameCode { get; set; }
    public string? HostConnectionId { get; set; }
    public string? GuestConnectionId { get; set; }
    public string? CurrentTurn { get; set; }
    public int MoveCount { get; set; }
}

public class TestEvent
{
    public string? EventName { get; set; }
    public DateTimeOffset Timestamp { get; set; }
}
