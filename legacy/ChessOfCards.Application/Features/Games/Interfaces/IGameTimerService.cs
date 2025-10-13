namespace ChessOfCards.Application.Features.Games;

public interface IGameTimerService
{
  public void InitTimers(string gameCode, int totalSeconds);

  public (double hostSecondsElapsed, double guestSecondsElapsed) StartHostTimer(string gameCode);

  public (double hostSecondsElapsed, double guestSecondsElapsed) StartGuestTimer(string gameCode);

  public void StartDisconnectTimer(string gameCode);

  public void StopDisconnectTimer(string gameCode);

  public (double hostSecondsElapsed, double guestSecondsElapsed) RemoveTimers(string gameCode);

  public (double hostSecondsElapsed, double guestSecondsElapsed) GetElapsedTime(string gameCode);
}
