namespace ChessOfCards.Domain.Features.Games;

public class PlayerGameView(
  int numCardsInOpponentsDeck,
  int numCardsInOpponentsHand,
  int numCardsInPlayersDeck,
  Hand hand,
  Lane[] lanes,
  bool isHost,
  bool isHostPlayersTurn,
  int? redJokerLaneIndex,
  int? blackJokerLaneIndex,
  DateTime gameCreatedTimestampUTC,
  List<MoveMade> movesMade,
  DurationOption durationOption,
  DateTime? gameEndedTimestampUTC,
  string gameCode,
  List<CandidateMove>? candidateMoves,
  bool hasEnded,
  List<ChatMessageView> chatMessages,
  double hostSecondsRemaining,
  double guestSecondsRemaining,
  PlayerOrNone wonBy,
  string? hostName,
  string? guestName,
  int numUnreadMessages
)
{
  public int NumCardsInOpponentsHand { get; set; } = numCardsInOpponentsHand;

  public int NumCardsInOpponentsDeck { get; set; } = numCardsInOpponentsDeck;

  public int NumCardsInPlayersDeck { get; set; } = numCardsInPlayersDeck;

  public Hand Hand { get; set; } = hand;

  public Lane[] Lanes { get; set; } = lanes;

  public bool IsHost { get; set; } = isHost;

  public bool IsHostPlayersTurn { get; set; } = isHostPlayersTurn;

  public int? RedJokerLaneIndex { get; set; } = redJokerLaneIndex;

  public int? BlackJokerLaneIndex { get; set; } = blackJokerLaneIndex;

  public DateTime GameCreatedTimestampUTC { get; set; } = gameCreatedTimestampUTC;

  public List<MoveMade> MovesMade { get; set; } = movesMade;

  public DurationOption DurationOption { get; set; } = durationOption;

  public DateTime? GameEndedTimestampUTC { get; set; } = gameEndedTimestampUTC;

  public string GameCode { get; set; } = gameCode;

  public List<CandidateMove>? CandidateMoves { get; set; } = candidateMoves;

  public bool HasEnded { get; set; } = hasEnded;

  public List<ChatMessageView> ChatMessages { get; set; } = chatMessages;

  public string? HostName { get; set; } = hostName;

  public string? GuestName { get; set; } = guestName;

  public double HostSecondsRemaining { get; set; } = hostSecondsRemaining;

  public double GuestSecondsRemaining { get; set; } = guestSecondsRemaining;

  public PlayerOrNone WonBy { get; set; } = wonBy;

  public int NumUnreadMessages { get; set; } = numUnreadMessages;
}
