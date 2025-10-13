using Amazon.Lambda.APIGatewayEvents;
using ChessOfCards.ConnectionHandler.Configuration;
using ChessOfCards.ConnectionHandler.Handlers;
using ChessOfCards.ConnectionHandler.Tests.Helpers;
using ChessOfCards.ConnectionHandler.Tests.Mocks;
using ChessOfCards.Infrastructure.Models;
using ChessOfCards.Infrastructure.Repositories;
using ChessOfCards.Infrastructure.Services;
using Moq;

namespace ChessOfCards.ConnectionHandler.Tests.Handlers;

public class RouteDispatcherTests
{
    private readonly Mock<IConnectionRepository> _mockConnectionRepo;
    private readonly Mock<IActiveGameRepository> _mockGameRepo;
    private readonly Mock<IGameTimerRepository> _mockTimerRepo;
    private readonly Mock<WebSocketService> _mockWebSocketService;
    private readonly RouteDispatcher _dispatcher;
    private readonly MockLambdaContext _context;

    public RouteDispatcherTests()
    {
        _mockConnectionRepo = new Mock<IConnectionRepository>();
        _mockGameRepo = new Mock<IActiveGameRepository>();
        _mockTimerRepo = new Mock<IGameTimerRepository>();
        _mockWebSocketService = new Mock<WebSocketService>(
            "wss://test.execute-api.us-east-1.amazonaws.com/dev"
        );

        // Setup default behavior for SendMessageAsync to return true
        _mockWebSocketService
            .Setup(ws => ws.SendMessageAsync(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync(true);

        var services = new ServiceDependencies(
            _mockConnectionRepo.Object,
            _mockGameRepo.Object,
            _mockTimerRepo.Object,
            _mockWebSocketService.Object
        );

        _dispatcher = new RouteDispatcher(services);
        _context = new MockLambdaContext();
    }

    [Fact]
    public async Task DispatchAsync_WithConnectRoute_Returns200()
    {
        // Arrange
        var connectionId = "test-connection-123";
        var request = TestHelpers.CreateConnectRequest(connectionId);

        _mockConnectionRepo
            .Setup(r => r.CreateAsync(It.IsAny<ConnectionRecord>()))
            .ReturnsAsync((ConnectionRecord c) => c);

        // Act
        var response = await _dispatcher.DispatchAsync("$connect", request, _context);

        // Assert
        Assert.Equal(200, response.StatusCode);
        _mockConnectionRepo.Verify(
            r => r.CreateAsync(It.Is<ConnectionRecord>(c => c.ConnectionId == connectionId)),
            Times.Once
        );
    }

    [Fact]
    public async Task DispatchAsync_WithDisconnectRoute_Returns200()
    {
        // Arrange
        var connectionId = "test-connection-123";
        var request = TestHelpers.CreateDisconnectRequest(connectionId);

        _mockConnectionRepo
            .Setup(r => r.GetByConnectionIdAsync(connectionId))
            .ReturnsAsync(new ConnectionRecord(connectionId));

        _mockConnectionRepo.Setup(r => r.DeleteAsync(connectionId)).ReturnsAsync(true);

        // Act
        var response = await _dispatcher.DispatchAsync("$disconnect", request, _context);

        // Assert
        Assert.Equal(200, response.StatusCode);
        _mockConnectionRepo.Verify(r => r.DeleteAsync(connectionId), Times.Once);
    }

    [Fact]
    public async Task DispatchAsync_WithUnknownRoute_Returns400()
    {
        // Arrange
        var request = TestHelpers.CreateConnectRequest("test-connection-123");

        // Act
        var response = await _dispatcher.DispatchAsync("$unknown", request, _context);

        // Assert
        Assert.Equal(400, response.StatusCode);
    }

    [Fact]
    public async Task HandleConnectAsync_CreatesConnectionRecord()
    {
        // Arrange
        var connectionId = "test-connection-456";
        var request = TestHelpers.CreateConnectRequest(connectionId);

        _mockConnectionRepo
            .Setup(r => r.CreateAsync(It.IsAny<ConnectionRecord>()))
            .ReturnsAsync((ConnectionRecord c) => c);

        // Act
        var response = await _dispatcher.DispatchAsync("$connect", request, _context);

        // Assert
        Assert.Equal(200, response.StatusCode);
        _mockConnectionRepo.Verify(
            r =>
                r.CreateAsync(
                    It.Is<ConnectionRecord>(c =>
                        c.ConnectionId == connectionId && c.GameCode == null
                    )
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task HandleConnectAsync_SendsConnectedMessage()
    {
        // Arrange
        var connectionId = "test-connection-789";
        var request = TestHelpers.CreateConnectRequest(connectionId);

        _mockConnectionRepo
            .Setup(r => r.CreateAsync(It.IsAny<ConnectionRecord>()))
            .ReturnsAsync((ConnectionRecord c) => c);

        // Act
        var response = await _dispatcher.DispatchAsync("$connect", request, _context);

        // Assert
        Assert.Equal(200, response.StatusCode);
        _mockWebSocketService.Verify(
            ws => ws.SendMessageAsync(connectionId, It.IsAny<object>()),
            Times.Once
        );
    }

    [Fact]
    public async Task HandleConnectAsync_OnError_Returns500()
    {
        // Arrange
        var connectionId = "test-connection-error";
        var request = TestHelpers.CreateConnectRequest(connectionId);

        _mockConnectionRepo
            .Setup(r => r.CreateAsync(It.IsAny<ConnectionRecord>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var response = await _dispatcher.DispatchAsync("$connect", request, _context);

        // Assert
        Assert.Equal(500, response.StatusCode);
    }

    [Fact]
    public async Task HandleDisconnectAsync_WithNoGameCode_DeletesConnection()
    {
        // Arrange
        var connectionId = "test-connection-no-game";
        var request = TestHelpers.CreateDisconnectRequest(connectionId);
        var connection = new ConnectionRecord(connectionId);

        _mockConnectionRepo
            .Setup(r => r.GetByConnectionIdAsync(connectionId))
            .ReturnsAsync(connection);

        _mockConnectionRepo.Setup(r => r.DeleteAsync(connectionId)).ReturnsAsync(true);

        // Act
        var response = await _dispatcher.DispatchAsync("$disconnect", request, _context);

        // Assert
        Assert.Equal(200, response.StatusCode);
        _mockConnectionRepo.Verify(r => r.DeleteAsync(connectionId), Times.Once);
        _mockGameRepo.Verify(r => r.GetByGameCodeAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task HandleDisconnectAsync_WithActiveGame_CreatesDisconnectTimer()
    {
        // Arrange
        var connectionId = "host-connection";
        var gameCode = "GAME123";
        var request = TestHelpers.CreateDisconnectRequest(connectionId);

        var connection = new ConnectionRecord(connectionId) { GameCode = gameCode };

        var game = new ActiveGameRecord
        {
            GameCode = gameCode,
            HostConnectionId = connectionId,
            GuestConnectionId = "guest-connection",
            HasEnded = false,
        };

        _mockConnectionRepo
            .Setup(r => r.GetByConnectionIdAsync(connectionId))
            .ReturnsAsync(connection);

        _mockGameRepo.Setup(r => r.GetByGameCodeAsync(gameCode)).ReturnsAsync(game);

        _mockGameRepo
            .Setup(r => r.UpdateAsync(It.IsAny<ActiveGameRecord>()))
            .ReturnsAsync((ActiveGameRecord g) => g);

        _mockTimerRepo
            .Setup(r => r.CreateAsync(It.IsAny<GameTimerRecord>()))
            .ReturnsAsync((GameTimerRecord t) => t);

        _mockConnectionRepo.Setup(r => r.DeleteAsync(connectionId)).ReturnsAsync(true);

        // Act
        var response = await _dispatcher.DispatchAsync("$disconnect", request, _context);

        // Assert
        Assert.Equal(200, response.StatusCode);
        _mockGameRepo.Verify(r => r.UpdateAsync(It.IsAny<ActiveGameRecord>()), Times.Once);
        _mockTimerRepo.Verify(
            r => r.CreateAsync(It.Is<GameTimerRecord>(t => t.GameCode == gameCode)),
            Times.Once
        );
        _mockWebSocketService.Verify(
            ws => ws.SendMessageAsync("guest-connection", It.IsAny<object>()),
            Times.Once
        );
    }

    [Fact]
    public async Task HandleDisconnectAsync_WithEndedGame_DoesNotCreateTimer()
    {
        // Arrange
        var connectionId = "host-connection";
        var gameCode = "ENDED123";
        var request = TestHelpers.CreateDisconnectRequest(connectionId);

        var connection = new ConnectionRecord(connectionId) { GameCode = gameCode };

        var game = new ActiveGameRecord
        {
            GameCode = gameCode,
            HostConnectionId = connectionId,
            GuestConnectionId = "guest-connection",
            HasEnded = true,
        };

        _mockConnectionRepo
            .Setup(r => r.GetByConnectionIdAsync(connectionId))
            .ReturnsAsync(connection);

        _mockGameRepo.Setup(r => r.GetByGameCodeAsync(gameCode)).ReturnsAsync(game);

        _mockConnectionRepo.Setup(r => r.DeleteAsync(connectionId)).ReturnsAsync(true);

        // Act
        var response = await _dispatcher.DispatchAsync("$disconnect", request, _context);

        // Assert
        Assert.Equal(200, response.StatusCode);
        _mockTimerRepo.Verify(r => r.CreateAsync(It.IsAny<GameTimerRecord>()), Times.Never);
        _mockWebSocketService.Verify(
            ws => ws.SendMessageAsync(It.IsAny<string>(), It.IsAny<object>()),
            Times.Never
        );
    }

    [Fact]
    public async Task HandleDisconnectAsync_WithNonExistentConnection_Returns200()
    {
        // Arrange
        var connectionId = "non-existent-connection";
        var request = TestHelpers.CreateDisconnectRequest(connectionId);

        _mockConnectionRepo
            .Setup(r => r.GetByConnectionIdAsync(connectionId))
            .ReturnsAsync((ConnectionRecord?)null);

        // Act
        var response = await _dispatcher.DispatchAsync("$disconnect", request, _context);

        // Assert
        Assert.Equal(200, response.StatusCode);
        _mockConnectionRepo.Verify(r => r.DeleteAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task HandleDisconnectAsync_OnError_Returns200()
    {
        // Arrange
        var connectionId = "error-connection";
        var request = TestHelpers.CreateDisconnectRequest(connectionId);

        _mockConnectionRepo
            .Setup(r => r.GetByConnectionIdAsync(connectionId))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var response = await _dispatcher.DispatchAsync("$disconnect", request, _context);

        // Assert
        // Disconnect should always return 200 even on errors to avoid retries
        Assert.Equal(200, response.StatusCode);
    }

    [Fact]
    public async Task HandleDisconnectAsync_GuestDisconnects_NotifiesHost()
    {
        // Arrange
        var guestConnectionId = "guest-connection";
        var gameCode = "GAME456";
        var request = TestHelpers.CreateDisconnectRequest(guestConnectionId);

        var connection = new ConnectionRecord(guestConnectionId) { GameCode = gameCode };

        var game = new ActiveGameRecord
        {
            GameCode = gameCode,
            HostConnectionId = "host-connection",
            GuestConnectionId = guestConnectionId,
            HasEnded = false,
        };

        _mockConnectionRepo
            .Setup(r => r.GetByConnectionIdAsync(guestConnectionId))
            .ReturnsAsync(connection);

        _mockGameRepo.Setup(r => r.GetByGameCodeAsync(gameCode)).ReturnsAsync(game);

        _mockGameRepo
            .Setup(r => r.UpdateAsync(It.IsAny<ActiveGameRecord>()))
            .ReturnsAsync((ActiveGameRecord g) => g);

        _mockTimerRepo
            .Setup(r => r.CreateAsync(It.IsAny<GameTimerRecord>()))
            .ReturnsAsync((GameTimerRecord t) => t);

        _mockConnectionRepo.Setup(r => r.DeleteAsync(guestConnectionId)).ReturnsAsync(true);

        // Act
        var response = await _dispatcher.DispatchAsync("$disconnect", request, _context);

        // Assert
        Assert.Equal(200, response.StatusCode);
        _mockWebSocketService.Verify(
            ws => ws.SendMessageAsync("host-connection", It.IsAny<object>()),
            Times.Once
        );
    }
}
