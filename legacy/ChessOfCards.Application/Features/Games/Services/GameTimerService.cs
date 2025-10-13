using System.Diagnostics;
using ChessOfCards.Domain.Features.Games;
using MediatR;

namespace ChessOfCards.Application.Features.Games;

public class GameTimers(Timer gameClockTimer, int totalSeconds)
{
  public readonly int TotalSeconds = totalSeconds;

  public Timer? DisconnectTimer { get; set; } = null;

  public readonly Stopwatch HostTimer = new();

  public readonly Stopwatch GuestTimer = new();

  public Timer GameClockTimer { get; set; } = gameClockTimer;
}

public class GameTimerService(IMediator mediator) : IGameTimerService
{
  private readonly Dictionary<string, GameTimers> GameCodesToGameTimers = [];

  private readonly IMediator _mediator = mediator;

  public void InitTimers(string gameCode, int totalSeconds)
  {
    var gameClockTimer = new Timer(
      OnGameClockTimerExpired,
      gameCode,
      TimeSpan.FromSeconds(totalSeconds),
      Timeout.InfiniteTimeSpan
    );

    var gameTimers = new GameTimers(gameClockTimer, totalSeconds);

    gameTimers.HostTimer.Start();

    GameCodesToGameTimers.Add(gameCode, gameTimers);
  }

  public void StartDisconnectTimer(string gameCode)
  {
    GameCodesToGameTimers.TryGetValue(gameCode, out var gameTimers);
    if (gameTimers is null)
    {
      return;
    }
    gameTimers.DisconnectTimer?.Dispose();

    gameTimers.DisconnectTimer = new Timer(
      OnDisconnectTimerExpired,
      gameCode,
      TimeSpan.FromSeconds(30),
      Timeout.InfiniteTimeSpan
    );
  }

  public void StopDisconnectTimer(string gameCode)
  {
    GameCodesToGameTimers.TryGetValue(gameCode, out var gameTimers);
    if (gameTimers is null)
    {
      return;
    }
    gameTimers.DisconnectTimer?.Dispose();
  }

  public (double hostSecondsElapsed, double guestSecondsElapsed) StartGuestTimer(string gameCode)
  {
    GameCodesToGameTimers.TryGetValue(gameCode, out var gameTimers);
    if (gameTimers is null)
    {
      return (0, 0);
    }

    var hostSecondsElapsed = gameTimers.HostTimer.Elapsed.TotalSeconds;
    var guestSecondsElapsed = gameTimers.GuestTimer.Elapsed.TotalSeconds;

    gameTimers.HostTimer.Stop();
    gameTimers.GuestTimer.Start();
    gameTimers.GameClockTimer = new Timer(
      OnGameClockTimerExpired,
      gameCode,
      TimeSpan.FromSeconds(gameTimers.TotalSeconds - guestSecondsElapsed),
      Timeout.InfiniteTimeSpan
    );

    return (hostSecondsElapsed, guestSecondsElapsed);
  }

  public (double hostSecondsElapsed, double guestSecondsElapsed) StartHostTimer(string gameCode)
  {
    GameCodesToGameTimers.TryGetValue(gameCode, out var gameTimers);
    if (gameTimers is null)
    {
      return (0, 0);
    }

    var hostSecondsElapsed = gameTimers.HostTimer.Elapsed.TotalSeconds;
    var guestSecondsElapsed = gameTimers.GuestTimer.Elapsed.TotalSeconds;

    gameTimers.GuestTimer.Stop();
    gameTimers.HostTimer.Start();
    gameTimers.GameClockTimer = new Timer(
      OnGameClockTimerExpired,
      gameCode,
      TimeSpan.FromSeconds(gameTimers.TotalSeconds - hostSecondsElapsed),
      Timeout.InfiniteTimeSpan
    );

    return (hostSecondsElapsed, guestSecondsElapsed);
  }

  public (double hostSecondsElapsed, double guestSecondsElapsed) RemoveTimers(string gameCode)
  {
    GameCodesToGameTimers.TryGetValue(gameCode, out var gameTimers);
    if (gameTimers is null)
    {
      return (0, 0);
    }

    gameTimers.DisconnectTimer?.Dispose();
    gameTimers.DisconnectTimer = null;
    gameTimers.HostTimer.Stop();
    gameTimers.GuestTimer.Stop();

    GameCodesToGameTimers.Remove(gameCode);

    var hostSecondsElapsed = gameTimers.HostTimer.Elapsed.Seconds;
    var guestTimerElapsedSeconds = gameTimers.GuestTimer.Elapsed.Seconds;

    return (hostSecondsElapsed, guestTimerElapsedSeconds);
  }

  private async void OnDisconnectTimerExpired(object? state)
  {
    if (state is null)
    {
      return;
    }

    var gameCode = (string)state;

    GameCodesToGameTimers.Remove(gameCode);

    await _mediator.Publish(new DisconnectTimerExpiredCommand(gameCode));
  }

  private async void OnGameClockTimerExpired(object? state)
  {
    if (state is null)
    {
      return;
    }

    var gameCode = (string)state;

    var (hostSecondsElapsed, guestSecondsElapsed) = GetElapsedTime(gameCode);

    GameCodesToGameTimers.Remove(gameCode);

    await _mediator.Publish(
      new GameClockTimerExpiredCommand(gameCode, hostSecondsElapsed, guestSecondsElapsed)
    );
  }

  public (double hostSecondsElapsed, double guestSecondsElapsed) GetElapsedTime(string gameCode)
  {
    GameCodesToGameTimers.TryGetValue(gameCode, out var gameTimers);
    if (gameTimers is null)
    {
      return (0, 0);
    }

    return (gameTimers.HostTimer.Elapsed.TotalSeconds, gameTimers.GuestTimer.Elapsed.TotalSeconds);
  }
}
