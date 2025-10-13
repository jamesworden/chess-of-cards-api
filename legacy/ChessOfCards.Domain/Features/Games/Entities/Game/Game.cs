using ChessOfCards.Domain.Shared.Util;

namespace ChessOfCards.Domain.Features.Games;

public class Game
{
  public PlayerOrNone WonBy { get; private set; } = PlayerOrNone.None;

  public bool IsHostPlayersTurn { get; private set; } = true;

  public string HostConnectionId { get; private set; }

  public string GuestConnectionId { get; private set; }

  public string GameCode { get; private set; }

  private Lane[] Lanes = [];

  private Player HostPlayer { get; set; }

  private Player GuestPlayer { get; set; }

  private int? RedJokerLaneIndex { get; set; }

  private int? BlackJokerLaneIndex { get; set; }

  private DateTime GameCreatedTimestampUTC { get; set; }

  private readonly List<MoveMade> MovesMade = [];

  public DurationOption DurationOption { get; private set; }

  private DateTime? GameEndedTimestampUTC { get; set; }

  private List<List<CandidateMove>> CandidateMoves { get; set; } = [];

  public bool HasEnded { get; private set; } = false;

  private List<ChatMessage> ChatMessages { get; set; } = [];

  private List<ChatMessageView> ChatMessageViews { get; set; } = [];

  private string? HostName { get; set; }

  private string? GuestName { get; set; }

  public DateTime? HostPlayerDisconnectedTimestampUTC { get; private set; } = null;

  public DateTime? GuestPlayerDisconnectedTimestampUTC { get; private set; } = null;

  private bool DrawOfferFromHost = false;

  private bool DrawOfferFromGuest = false;

  private int? HostsLatestReadChatMessageIndex = null;

  private int? GuestsLatestReadChatMessageIndex = null;

  public Game(
    string hostConnectionId,
    string guestConnectionId,
    string gameCode,
    DurationOption durationOption,
    string? hostName,
    string? guestName
  )
  {
    var playerDecks = new Deck().Shuffle().Split();

    var hostDeck = playerDecks.Item1;
    var guestDeck = playerDecks.Item2;

    var hostHandCards = hostDeck.DrawCards(5);
    var guestHandCards = guestDeck.DrawCards(5);

    var hostHand = new Hand(hostHandCards);
    var guestHand = new Hand(guestHandCards);

    var hostPlayer = new Player(hostDeck, hostHand);
    var guestPlayer = new Player(guestDeck, guestHand);

    var lanes = CreateEmptyLanes();

    var gameCreatedTimestampUTC = DateTime.UtcNow;

    HostConnectionId = hostConnectionId;
    GuestConnectionId = guestConnectionId;
    GameCode = gameCode;
    HostPlayer = hostPlayer;
    GuestPlayer = guestPlayer;
    Lanes = lanes;
    GameCreatedTimestampUTC = gameCreatedTimestampUTC;
    DurationOption = durationOption;
    GameEndedTimestampUTC = null;
    HostName = hostName;
    GuestName = guestName;

    CandidateMoves.Add(GetCandidateMoves(true, true));
  }

  private Lane[] CreateEmptyLanes()
  {
    Lane[] lanes = new Lane[5];

    for (int i = 0; i < lanes.Length; i++)
    {
      lanes[i] = CreateEmptyLane();
    }

    return lanes;
  }

  private Lane CreateEmptyLane()
  {
    var rows = CreateEmptyRows();
    Lane lane = new(rows);

    return lane;
  }

  private List<Card>[] CreateEmptyRows()
  {
    List<Card>[] rows = new List<Card>[7];

    for (int i = 0; i < rows.Length; i++)
    {
      var row = new List<Card>();
      rows[i] = row;
    }

    return rows;
  }

  public void SetLanes(Lane[] lanes)
  {
    Lanes = lanes;
  }

  public void SetHostHand(Hand hand)
  {
    HostPlayer.Hand = hand;
  }

  public List<CandidateMove> GetCandidateMoves(bool asHostPlayer, bool isHostPlayersTurn)
  {
    var player = asHostPlayer ? HostPlayer : GuestPlayer;
    var candidateMoves = new List<CandidateMove>();

    foreach (var card in player.Hand.Cards)
    {
      var cardCandidateMoves = GetCandidateMoves(card, asHostPlayer, isHostPlayersTurn);
      candidateMoves.AddRange(cardCandidateMoves);
    }

    return candidateMoves;
  }

  private List<CandidateMove> GetCandidateMoves(
    Card card,
    bool asHostPlayer,
    bool isHostPlayersTurn
  )
  {
    var candidateMoves = new List<CandidateMove>();

    for (var rowIndex = 0; rowIndex < 7; rowIndex++)
    {
      for (var laneIndex = 0; laneIndex < 5; laneIndex++)
      {
        var placeCardAttempt = new PlaceCardAttempt(card, laneIndex, rowIndex);
        var placeCardAttempts = new List<PlaceCardAttempt> { placeCardAttempt };
        var move = new Move(placeCardAttempts);
        var candidateMove = GetCandidateMove(move, asHostPlayer, isHostPlayersTurn);
        candidateMoves.Add(candidateMove);

        if (placeCardAttempt.IsDefensive(asHostPlayer))
        {
          var player = asHostPlayer ? HostPlayer : GuestPlayer;
          var cardsInHand = player.Hand.Cards;
          var placeMultipleCandidateMoves = GetPlaceMultipleCandidateMoves(
            placeCardAttempt,
            cardsInHand,
            asHostPlayer,
            isHostPlayersTurn
          );
          candidateMoves.AddRange(placeMultipleCandidateMoves);
        }
      }
    }

    return candidateMoves;
  }

  private CandidateMove GetCandidateMove(Move move, bool asHostPlayer, bool isHostPlayersTurn)
  {
    var invalidReason = GetReasonIfMoveInvalid(move, asHostPlayer, isHostPlayersTurn);
    var isValid = invalidReason is null;
    return new CandidateMove(move, isValid, invalidReason);
  }

  private List<CandidateMove> GetPlaceMultipleCandidateMoves(
    PlaceCardAttempt initialPlaceCardAttempt,
    List<Card> cardsInHand,
    bool asHostPlayer,
    bool isHostPlayersTurn
  )
  {
    var candidateMoves = new List<CandidateMove>();

    var candidateCardsInHand = cardsInHand
      .Where(cardInHand =>
        initialPlaceCardAttempt.Card.KindMatches(cardInHand)
        && !initialPlaceCardAttempt.Card.SuitMatches(cardInHand)
      )
      .ToList();

    List<List<Card>> candidateCardPermutationSubsets = PermutationsUtil.GetSubsetsPermutations(
      candidateCardsInHand
    );

    foreach (var candidateCards in candidateCardPermutationSubsets)
    {
      var totalCandidatePlaceCardAttempts = new List<PlaceCardAttempt> { initialPlaceCardAttempt };
      var candidatePlaceCardAttempts = new List<PlaceCardAttempt>();

      for (var i = 0; i < candidateCards.Count; i++)
      {
        var rowIndex = asHostPlayer
          ? initialPlaceCardAttempt.TargetRowIndex + 1 + i
          : initialPlaceCardAttempt.TargetRowIndex - 1 - i;

        // When placing multiple cards beyond the middle of the lane, the target row index of 3
        // is skipped. For example, the host might play one move from row indexes 1, 2, and 4,
        // while the guest might play one move from row indexes 5, 4, 2.
        if (asHostPlayer && rowIndex >= 3)
        {
          rowIndex++;
        }
        else if (!asHostPlayer && rowIndex <= 3)
        {
          rowIndex--;
        }

        var card = candidateCards[i];
        if (card is not null)
        {
          candidatePlaceCardAttempts.Add(
            new PlaceCardAttempt(card, initialPlaceCardAttempt.TargetLaneIndex, rowIndex)
          );
        }
      }

      totalCandidatePlaceCardAttempts.AddRange(candidatePlaceCardAttempts);
      var move = new Move(totalCandidatePlaceCardAttempts);
      var candidateMove = GetCandidateMove(move, asHostPlayer, isHostPlayersTurn);
      candidateMoves.Add(candidateMove);
    }

    return candidateMoves;
  }

  public void End(PlayerOrNone wonBy = PlayerOrNone.None)
  {
    HasEnded = true;
    GameEndedTimestampUTC = DateTime.UtcNow;
    WonBy = wonBy;
  }

  public bool MoveIsLatestCandidate(Move move)
  {
    var lastCandidateMoves = CandidateMoves.LastOrDefault();

    var moveIsOneOfLastCandidates =
      lastCandidateMoves?.Select(candidateMove => candidateMove.Move)?.Any(move.MovesMatch)
      ?? false;

    return lastCandidateMoves is not null && moveIsOneOfLastCandidates;
  }

  public List<OfferDrawResults> OfferDraw(string connectionId)
  {
    var isHost = connectionId == HostConnectionId;
    var alreadyOfferedDraw = (isHost && DrawOfferFromHost) || (!isHost && DrawOfferFromGuest);
    if (alreadyOfferedDraw)
    {
      return [OfferDrawResults.AlreadyOfferedDraw];
    }

    if (isHost)
    {
      DrawOfferFromHost = true;
    }
    else
    {
      DrawOfferFromGuest = true;
    }

    return [];
  }

  public string? GetReasonIfMoveInvalid(Move move, bool asHostPlayer, bool isHostPlayersTurn)
  {
    if (!((asHostPlayer && isHostPlayersTurn) || (!asHostPlayer && !isHostPlayersTurn)))
    {
      return "It's not your turn!";
    }
    if (move.PlaceCardAttempts.Count == 0)
    {
      return "You need to place a card!";
    }
    if (move.PlaceCardAttempts.Count > 4)
    {
      return "You placed too many cards!";
    }
    if (move.PlaceCardAttempts.Select(attempt => attempt.TargetLaneIndex).Distinct().Count() > 1)
    {
      return "You can't place cards on different lanes!";
    }
    if (
      move.PlaceCardAttempts.Select(attempt => attempt.TargetRowIndex).Distinct().Count()
      < move.PlaceCardAttempts.Count
    )
    {
      return "You can't place cards on the same position!";
    }
    if (TargetLaneHasBeenWon(move))
    {
      return "This lane was won already!";
    }
    if (move.PlaceCardAttempts.Any(placeCardAttempt => placeCardAttempt.TargetRowIndex == 3))
    {
      return "You can't place a card in the middle!";
    }
    if (!move.ContainsConsecutivePlaceCardAttempts())
    {
      return "You can't place cards that are separate from one another!";
    }
    if (move.PlaceCardAttempts.Select(attempt => attempt.Card.Kind).Distinct().Count() > 1)
    {
      return "Placing multiple cards must be of the same kind!";
    }
    if (
      move.PlaceCardAttempts.Select(attempt => attempt.Card.Suit).Distinct().Count()
      < move.PlaceCardAttempts.Count
    )
    {
      return "Placing multiple cards must be of a different suit!";
    }
    if (TriedToCaptureDistantRow(move, asHostPlayer))
    {
      return "You can't capture this position yet!";
    }
    if (TriedToCaptureGreaterCard(move, asHostPlayer))
    {
      return "You can't capture a greater card!";
    }
    if (StartedMovePlayerSide(move, asHostPlayer) && PlayerHasAdvantage(move, asHostPlayer))
    {
      return "You must attack this lane!";
    }
    if (StartedMoveOpponentSide(move, asHostPlayer) && OpponentHasAdvantage(move, asHostPlayer))
    {
      return "You must defend this lane!";
    }
    if (StartedMoveOpponentSide(move, asHostPlayer) && LaneHasNoAdvantage(move))
    {
      return "You aren't ready to attack here yet.";
    }
    if (!SuitOrKindMatchesLastCardPlayed(move, asHostPlayer))
    {
      return "This card can't be placed here.";
    }
    if (TriedToReinforceGreaterCard(move, asHostPlayer))
    {
      return "You can't reinforce a greater card!";
    }
    return null;
  }

  public bool TargetLaneHasBeenWon(Move move)
  {
    foreach (var placeCardAttempt in move.PlaceCardAttempts)
    {
      var lane = Lanes[placeCardAttempt.TargetLaneIndex];
      if (lane.WonBy != PlayerOrNone.None)
      {
        return true;
      }
    }
    return false;
  }

  public bool TriedToCaptureDistantRow(Move move, bool asHostPlayer)
  {
    var firstPlaceCardAttempt = move.GetInitialPlaceCardAttempt(asHostPlayer);
    if (firstPlaceCardAttempt is null)
    {
      return false;
    }

    if (asHostPlayer)
    {
      var startIndex = StartedMovePlayerSide(move, asHostPlayer) ? 0 : 4;
      return !CapturedAllPreviousRows(firstPlaceCardAttempt, startIndex, asHostPlayer);
    }

    var endIndex = StartedMoveOpponentSide(move, asHostPlayer) ? 2 : 6;
    return !CapturedAllFollowingRows(firstPlaceCardAttempt, endIndex, asHostPlayer);
  }

  public bool StartedMovePlayerSide(Move move, bool playerIsHost)
  {
    var firstPlaceCardAttempt = move.PlaceCardAttempts.FirstOrDefault();
    if (firstPlaceCardAttempt is null)
    {
      return false;
    }
    return playerIsHost
      ? firstPlaceCardAttempt.TargetRowIndex < 3
      : firstPlaceCardAttempt.TargetRowIndex > 3;
  }

  public bool StartedMoveOpponentSide(Move move, bool playerIsHost)
  {
    var firstPlaceCardAttempt = move.PlaceCardAttempts.FirstOrDefault();
    if (firstPlaceCardAttempt is null)
    {
      return false;
    }
    return playerIsHost
      ? firstPlaceCardAttempt.TargetRowIndex > 3
      : firstPlaceCardAttempt.TargetRowIndex < 3;
  }

  /// <summary>
  /// Return true if there were no previous rows to capture
  /// </summary>
  public bool CapturedAllPreviousRows(
    PlaceCardAttempt placeCardAttempt,
    int startIndex,
    bool playerIsHost
  )
  {
    var targetLaneIndex = placeCardAttempt.TargetLaneIndex;
    var targetRowIndex = placeCardAttempt.TargetRowIndex;
    var lane = Lanes[targetLaneIndex];

    if (placeCardAttempt.TargetRowIndex == startIndex)
    {
      return true;
    }

    for (int i = startIndex; i < targetRowIndex; i++)
    {
      var previousRow = lane.Rows[i];
      var previousRowNotOccupied = previousRow.Count == 0;

      if (previousRowNotOccupied)
      {
        return false;
      }

      var topCard = previousRow[previousRow.Count - 1];
      var topCardPlayedByPlayer = playerIsHost
        ? topCard.PlayedBy == PlayerOrNone.Host
        : topCard.PlayedBy == PlayerOrNone.Guest;

      if (!topCardPlayedByPlayer)
      {
        return false;
      }
    }

    return true;
  }

  /// <summary>
  /// Return true if there were no following rows to capture
  /// </summary>
  public bool CapturedAllFollowingRows(
    PlaceCardAttempt placeCardAttempt,
    int endIndex,
    bool playerIsHost
  )
  {
    var targetLaneIndex = placeCardAttempt.TargetLaneIndex;
    var targetRowIndex = placeCardAttempt.TargetRowIndex;
    var lane = Lanes[targetLaneIndex];

    if (placeCardAttempt.TargetRowIndex == endIndex)
    {
      return true;
    }

    for (int i = endIndex; i > targetRowIndex; i--)
    {
      var followingRow = lane.Rows[i];
      var followingRowNotOccupied = followingRow.Count == 0;

      if (followingRowNotOccupied)
      {
        return false;
      }

      var topCard = followingRow[^1];
      var topCardPlayedByPlayer = playerIsHost
        ? topCard.PlayedBy == PlayerOrNone.Host
        : topCard.PlayedBy == PlayerOrNone.Guest;

      if (!topCardPlayedByPlayer)
      {
        return false;
      }
    }

    return true;
  }

  public bool TriedToCaptureGreaterCard(Move move, bool playerIsHost)
  {
    var firstPlaceCardAttempt = move.GetInitialPlaceCardAttempt(playerIsHost);
    if (firstPlaceCardAttempt is null)
    {
      return false;
    }

    var card = firstPlaceCardAttempt.Card;
    var targetLaneIndex = firstPlaceCardAttempt.TargetLaneIndex;
    var targetRowIndex = firstPlaceCardAttempt.TargetRowIndex;
    var targetRow = Lanes[targetLaneIndex].Rows[targetRowIndex];
    if (targetRow.Count <= 0)
    {
      return false;
    }

    var targetCard = targetRow[^1];
    var suitsMatch = targetCard.Suit == card.Suit;
    var targetCardIsGreater = !card.Trumps(targetCard);
    var playerPlayedCard =
      targetCard.PlayedBy == (playerIsHost ? PlayerOrNone.Host : PlayerOrNone.Guest);

    return suitsMatch && targetCardIsGreater && !playerPlayedCard;
  }

  public bool PlayerHasAdvantage(Move move, bool playerIsHost)
  {
    var targetLaneIndex = move.PlaceCardAttempts[0].TargetLaneIndex;
    return Lanes[targetLaneIndex].LaneAdvantage
      == (playerIsHost ? PlayerOrNone.Host : PlayerOrNone.Guest);
  }

  public bool OpponentHasAdvantage(Move move, bool playerIsHost)
  {
    var targetLaneIndex = move.PlaceCardAttempts[0].TargetLaneIndex;
    return Lanes[targetLaneIndex].LaneAdvantage
      == (playerIsHost ? PlayerOrNone.Guest : PlayerOrNone.Host);
  }

  public bool LaneHasNoAdvantage(Move move)
  {
    return Lanes[move.PlaceCardAttempts[0].TargetLaneIndex].LaneAdvantage == PlayerOrNone.None;
  }

  /// <summary>
  /// Returns true if the target lane has no last card played.
  /// </summary>
  public bool SuitOrKindMatchesLastCardPlayed(Move move, bool playerIsHost)
  {
    var firstAttempt = move.GetInitialPlaceCardAttempt(playerIsHost);
    if (firstAttempt is null)
    {
      return true;
    }

    var lastCardPlayedInLane = Lanes[firstAttempt.TargetLaneIndex].LastCardPlayed;
    if (lastCardPlayedInLane is null)
    {
      return true;
    }

    return firstAttempt.Card.SuitOrKindMatch(lastCardPlayedInLane);
  }

  public bool TriedToReinforceGreaterCard(Move move, bool playerIsHost)
  {
    var firstAttempt = move.GetInitialPlaceCardAttempt(playerIsHost);
    if (firstAttempt is null)
    {
      return false;
    }

    var targetRow = Lanes[firstAttempt.TargetLaneIndex].Rows[firstAttempt.TargetRowIndex];
    var targetCard = targetRow.LastOrDefault();
    if (targetCard is null)
    {
      return false;
    }

    var playerPlayedTargetCard = playerIsHost
      ? targetCard.PlayedBy == PlayerOrNone.Host
      : targetCard.PlayedBy == PlayerOrNone.Guest;

    return playerPlayedTargetCard
      && targetCard.SuitMatches(firstAttempt.Card)
      && targetCard.Trumps(firstAttempt.Card);
  }

  public (bool reconnected, bool asHost) ReconnectPlayer(string connectionId, string? name)
  {
    if (HostPlayerIsDisconnected())
    {
      ReconnectHostPlayer(connectionId, name);
      return (true, true);
    }
    else if (GuestPlayerIsDisconnected())
    {
      ReconnectGuestPlayer(connectionId, name);
      return (true, false);
    }
    else
    {
      return (false, false);
    }
  }

  private bool HostPlayerIsDisconnected()
  {
    return HostPlayerDisconnectedTimestampUTC is not null;
  }

  private void ReconnectHostPlayer(string connectionId, string? hostName)
  {
    HostConnectionId = connectionId;
    HostPlayerDisconnectedTimestampUTC = null;
    HostName = hostName ?? HostName;
  }

  private bool GuestPlayerIsDisconnected()
  {
    return GuestPlayerDisconnectedTimestampUTC is not null;
  }

  private void ReconnectGuestPlayer(string connectionId, string? guestName)
  {
    GuestConnectionId = connectionId;
    GuestPlayerDisconnectedTimestampUTC = null;
    GuestName = guestName ?? GuestName;
  }

  public PlayerGameView ToHostPlayerView(double hostSecondsElapsed, double guestSecondsElapsed)
  {
    var candidateMoves =
      (IsHostPlayersTurn && CandidateMoves.Count != 0) ? CandidateMoves.LastOrDefault() : [];

    return new PlayerGameView(
      GuestPlayer.Deck.Cards.Count,
      GuestPlayer.Hand.Cards.Count,
      HostPlayer.Deck.Cards.Count,
      HostPlayer.Hand,
      Lanes,
      true,
      IsHostPlayersTurn,
      RedJokerLaneIndex,
      BlackJokerLaneIndex,
      GameCreatedTimestampUTC,
      GetPlayerViewMovesMade(true),
      DurationOption,
      GameEndedTimestampUTC,
      GameCode,
      candidateMoves,
      HasEnded,
      ChatMessageViews,
      DurationOption.ToSeconds() - hostSecondsElapsed,
      DurationOption.ToSeconds() - guestSecondsElapsed,
      WonBy,
      HostName,
      GuestName,
      GetNumUnreadMessages(true)
    );
  }

  public PlayerGameView ToGuestPlayerView(double hostSecondsElapsed, double guestSecondsElapsed)
  {
    var candidateMoves =
      (!IsHostPlayersTurn && CandidateMoves.Count != 0) ? CandidateMoves.LastOrDefault() : [];

    return new PlayerGameView(
      HostPlayer.Deck.Cards.Count,
      HostPlayer.Hand.Cards.Count,
      GuestPlayer.Deck.Cards.Count,
      GuestPlayer.Hand,
      Lanes,
      false,
      IsHostPlayersTurn,
      RedJokerLaneIndex,
      BlackJokerLaneIndex,
      GameCreatedTimestampUTC,
      GetPlayerViewMovesMade(false),
      DurationOption,
      GameEndedTimestampUTC,
      GameCode,
      candidateMoves,
      HasEnded,
      ChatMessageViews,
      DurationOption.ToSeconds() - hostSecondsElapsed,
      DurationOption.ToSeconds() - guestSecondsElapsed,
      WonBy,
      HostName,
      GuestName,
      GetNumUnreadMessages(false)
    );
  }

  private int GetNumUnreadMessages(bool asHost)
  {
    var latestReadIndex = asHost
      ? HostsLatestReadChatMessageIndex
      : GuestsLatestReadChatMessageIndex;

    if (latestReadIndex is null)
    {
      return ChatMessageViews.Count;
    }

    return ChatMessageViews.Count - 1 - (int)latestReadIndex;
  }

  private List<MoveMade> GetPlayerViewMovesMade(bool isHost)
  {
    return MovesMade
      .Select(moveMade =>
      {
        var newMoveMade = new MoveMade(moveMade.PlayedBy, moveMade.Move, moveMade.TimestampUTC, [])
        {
          CardMovements = moveMade
            .CardMovements.Select(movementBurstMade =>
            {
              return movementBurstMade
                .Select(cardMovement =>
                {
                  var fromHostDeckAndIsGuest = cardMovement.From.HostDeck && !isHost;
                  var fromGuestDeckAndIsHost = cardMovement.From.GuestDeck && isHost;
                  var isOpponentDrawnCardMovement =
                    fromHostDeckAndIsGuest || fromGuestDeckAndIsHost;

                  var newCardMovement = new CardMovement(
                    cardMovement.From,
                    cardMovement.To,
                    isOpponentDrawnCardMovement ? null : cardMovement.Card,
                    cardMovement.Notation
                  );

                  return newCardMovement;
                })
                .ToList();
            })
            .ToList()
        };

        return newMoveMade;
      })
      .ToList();
  }

  public bool IsPlayersTurn(string connectionId)
  {
    return (IsHostPlayersTurn && HostConnectionId == connectionId)
      || (!IsHostPlayersTurn && GuestConnectionId == connectionId);
  }

  public List<PassMoveResults> PassMove(string connectionId)
  {
    if (!IsPlayersTurn(connectionId))
    {
      return [PassMoveResults.NotPlayersTurn];
    }

    var cardMovements = DrawCardsUntil(IsHostPlayersTurn, 5);
    var move = new Move([]);
    var playedBy = IsHostPlayersTurn ? PlayerOrNone.Host : PlayerOrNone.Guest;
    var timeStampUTC = DateTime.UtcNow;
    MovesMade.Add(new MoveMade(playedBy, move, timeStampUTC, cardMovements, true));

    if (HasThreeBackToBackPasses())
    {
      End();
      return [];
    }

    SetNextPlayersTurn();
    var candidateMoves = GetCandidateMoves(IsHostPlayersTurn, IsHostPlayersTurn);
    CandidateMoves.Add(candidateMoves);
    return [];
  }

  private List<List<CardMovement>> DrawCardsUntil(bool forHost, int maxNumCards)
  {
    var player = forHost ? HostPlayer : GuestPlayer;
    var numCardsInPlayersHand = player.Hand.Cards.Count;
    var numCardsNeeded = maxNumCards - numCardsInPlayersHand;

    return numCardsNeeded > 0 ? DrawCardsFromDeck(forHost, numCardsNeeded) : [];
  }

  private List<List<CardMovement>> DrawCardsFromDeck(bool forHost, int numCardsToDraw)
  {
    var cardMovements = new List<List<CardMovement>>();
    var player = forHost ? HostPlayer : GuestPlayer;

    for (int i = 0; i < numCardsToDraw; i++)
    {
      var cardFromDeck = player.Deck.DrawCard();

      if (cardFromDeck is null)
      {
        return cardMovements;
      }

      var index = player.Hand.Cards.Count;

      var from = new CardStore() { HostDeck = forHost, GuestDeck = !forHost };

      var to = new CardStore()
      {
        HostHandCardIndex = forHost ? index : null,
        GuestHandCardIndex = forHost ? null : index
      };

      var cardMovement = new CardMovement(from, to, cardFromDeck);
      var cardMovementList = new List<CardMovement>() { cardMovement };
      cardMovements.Add(cardMovementList);

      player.Hand.AddCard(cardFromDeck);
    }

    return cardMovements;
  }

  private bool HasThreeBackToBackPasses()
  {
    if (MovesMade.Count < 6)
    {
      return false;
    }

    for (var i = MovesMade.Count - 1; i >= MovesMade.Count - 6; i--)
    {
      var moveMade = MovesMade[i];
      if (!moveMade.PassedMove)
      {
        return false;
      }
    }

    return true;
  }

  private void SetNextPlayersTurn()
  {
    DrawOfferFromGuest = false;
    DrawOfferFromHost = false;

    IsHostPlayersTurn = !IsHostPlayersTurn;
  }

  public void MarkPlayerAsDisconnected(string connectionId)
  {
    var hostPlayerIsDisconnected = connectionId == HostConnectionId;
    if (hostPlayerIsDisconnected)
    {
      HostPlayerDisconnectedTimestampUTC = DateTime.UtcNow;
    }
    else
    {
      GuestPlayerDisconnectedTimestampUTC = DateTime.UtcNow;
    }

    var bothPlayersDisconnected =
      HostPlayerDisconnectedTimestampUTC is not null
      && GuestPlayerDisconnectedTimestampUTC is not null;
    if (bothPlayersDisconnected)
    {
      End();
      return;
    }

    return;
  }

  public List<MakeMoveResults> MakeMove(
    string connectionId,
    Move move,
    List<Card>? rearrangedCardsInHand
  )
  {
    if (!MoveIsLatestCandidate(move))
    {
      return [MakeMoveResults.InvalidMove];
    }

    var playerIsHost = HostConnectionId == connectionId;
    var cardMovements = PlaceCardsAndApplyGameRules(move.PlaceCardAttempts, playerIsHost);

    if (rearrangedCardsInHand is not null)
    {
      var (hand, rearrangeHandResults) = RearrangeHand(connectionId, rearrangedCardsInHand);

      if (rearrangeHandResults.Contains(RearrangeHandResults.InvalidCards))
      {
        return [MakeMoveResults.InvalidMove];
      }
    }

    var placedMultipleCards = move.PlaceCardAttempts.Count > 1;
    var drawnCardMovements = placedMultipleCards
      ? DrawCardsFromDeck(playerIsHost, 1)
      : DrawCardsUntil(playerIsHost, 5);
    cardMovements.AddRange(drawnCardMovements);

    var playedBy = playerIsHost ? PlayerOrNone.Host : PlayerOrNone.Guest;
    var moveMade = new MoveMade(playedBy, move, DateTime.UtcNow, cardMovements);
    MovesMade.Add(moveMade);

    var opponentCandidateMoves = GetCandidateMoves(!IsHostPlayersTurn, !IsHostPlayersTurn);
    var anyValidOpponentCandidateMoves = opponentCandidateMoves.Any(move => move.IsValid);
    var playerCandidateMoves = GetCandidateMoves(IsHostPlayersTurn, IsHostPlayersTurn);
    var anyValidPlayerCandidateMoves = playerCandidateMoves.Any(move => move.IsValid);
    var results = new List<MakeMoveResults>();

    if ((!placedMultipleCards && anyValidOpponentCandidateMoves) || !anyValidPlayerCandidateMoves)
    {
      SetNextPlayersTurn();
      CandidateMoves.Add(opponentCandidateMoves);
    }
    else
    {
      CandidateMoves.Add(playerCandidateMoves);
    }

    if (!anyValidOpponentCandidateMoves)
    {
      results.Add(
        IsHostPlayersTurn
          ? MakeMoveResults.GuestTurnSkippedNoMoves
          : MakeMoveResults.HostTurnSkippedNoMoves
      );
    }

    if (!anyValidPlayerCandidateMoves && !anyValidOpponentCandidateMoves)
    {
      End();
    }

    return results;
  }

  private List<List<CardMovement>> PlaceCardsAndApplyGameRules(
    List<PlaceCardAttempt> placeCardAttempts,
    bool playerIsHost
  )
  {
    return placeCardAttempts
      .SelectMany(placeCardAttempt => PlaceCardAndApplyGameRules(placeCardAttempt, playerIsHost))
      .ToList();
  }

  private List<List<CardMovement>> PlaceCardAndApplyGameRules(
    PlaceCardAttempt placeCardAttempt,
    bool playerIsHost
  )
  {
    var initialCardMovements = new List<CardMovement> { PlaceCard(placeCardAttempt, playerIsHost) };
    var cardMovements = new List<List<CardMovement>> { initialCardMovements };

    var aceRuleCardMovements = TriggerAceRuleIfAppropriate(placeCardAttempt, playerIsHost);
    if (aceRuleCardMovements.Count != 0)
    {
      cardMovements.Add(aceRuleCardMovements);
      return cardMovements;
    }

    var capturedMiddleCardMovements = CaptureMiddleIfAppropriate(placeCardAttempt, playerIsHost);
    if (capturedMiddleCardMovements.Count != 0)
    {
      cardMovements.AddRange(capturedMiddleCardMovements);
      return cardMovements;
    }

    var laneWonCardMovements = WinLaneAndOrGameIfAppropriate(placeCardAttempt, playerIsHost);
    if (laneWonCardMovements.Count != 0)
    {
      cardMovements.Add(laneWonCardMovements);
    }

    return cardMovements;
  }

  private List<CardMovement> WinLaneAndOrGameIfAppropriate(
    PlaceCardAttempt placeCardAttempt,
    bool playerIsHost
  )
  {
    var placeCardInLastRow = playerIsHost
      ? placeCardAttempt.TargetRowIndex == 6
      : placeCardAttempt.TargetRowIndex == 0;

    if (!placeCardInLastRow)
    {
      return [];
    }

    Lanes[placeCardAttempt.TargetLaneIndex].WonBy = playerIsHost
      ? PlayerOrNone.Host
      : PlayerOrNone.Guest;

    var lane = Lanes[placeCardAttempt.TargetLaneIndex];
    var allCardsInLaneWithRowIndexes = GrabAllCardsFromLane(lane);
    var allCardsInLane = allCardsInLaneWithRowIndexes
      .Select(cardWithRowIndex => cardWithRowIndex.Item1)
      .ToList();

    var player = playerIsHost ? HostPlayer : GuestPlayer;
    player.Deck.Cards.AddRange(allCardsInLane);
    player.Deck.Shuffle();

    if (RedJokerLaneIndex is null)
    {
      RedJokerLaneIndex = placeCardAttempt.TargetLaneIndex;
    }
    else
    {
      BlackJokerLaneIndex = placeCardAttempt.TargetLaneIndex;
    }

    WinGameIfAppropriate();

    return GetCardMovementsFromWonCards(
      allCardsInLaneWithRowIndexes,
      placeCardAttempt,
      playerIsHost
    );
  }

  private bool WinGameIfAppropriate()
  {
    var lanesWonByHost = Lanes.Where(lane => lane.WonBy == PlayerOrNone.Host);
    var hostWon = lanesWonByHost.Count() == 2;
    if (hostWon)
    {
      End(PlayerOrNone.Host);
      return true;
    }

    var lanesWonByGuest = Lanes.Where(lane => lane.WonBy == PlayerOrNone.Guest);
    var guestWon = lanesWonByGuest.Count() == 2;
    if (guestWon)
    {
      End(PlayerOrNone.Guest);
      return true;
    }

    return false;
  }

  private List<List<CardMovement>> CaptureMiddleIfAppropriate(
    PlaceCardAttempt placeCardAttempt,
    bool playerIsHost
  )
  {
    var cardIsLastOnPlayerSide = playerIsHost
      ? placeCardAttempt.TargetRowIndex == 2
      : placeCardAttempt.TargetRowIndex == 4;

    if (!cardIsLastOnPlayerSide)
    {
      return [];
    }

    var lane = Lanes[placeCardAttempt.TargetLaneIndex];

    var noAdvantage = lane.LaneAdvantage == PlayerOrNone.None;
    if (noAdvantage)
    {
      return [CaptureNoAdvantageLane(lane, placeCardAttempt, playerIsHost)];
    }

    var opponentAdvantage = playerIsHost
      ? lane.LaneAdvantage == PlayerOrNone.Guest
      : lane.LaneAdvantage == PlayerOrNone.Host;
    if (opponentAdvantage)
    {
      return CaptureOpponentAdvantageLane(placeCardAttempt, playerIsHost);
    }

    return [];
  }

  private List<List<CardMovement>> CaptureOpponentAdvantageLane(
    PlaceCardAttempt placeCardAttempt,
    bool playerIsHost
  )
  {
    var lane = Lanes[placeCardAttempt.TargetLaneIndex];
    var topCardsWithRowIndexes = GrabTopCardsOfFirstThreeRows(lane, playerIsHost);
    var topCards = topCardsWithRowIndexes
      .Select(cardsWithRowIndexes => cardsWithRowIndexes.Item1)
      .ToList();
    var remainingCardsInLaneWithRowIndexes = GrabAllCardsFromLane(lane);
    var remainingCardsInLane = remainingCardsInLaneWithRowIndexes.Select(x => x.Item1).ToList();

    var middleRow = lane.Rows[3];
    middleRow.AddRange(topCards);

    var player = playerIsHost ? HostPlayer : GuestPlayer;
    player.Deck.Cards.AddRange(remainingCardsInLane);
    player.Deck.Shuffle();
    lane.LaneAdvantage = playerIsHost ? PlayerOrNone.Host : PlayerOrNone.Guest;

    var cardMovements = GetCardMovementsGoingToTheMiddle(topCardsWithRowIndexes, placeCardAttempt);
    cardMovements.AddRange(
      GetCapturedCardMovementsGoingToTheDeck(
        remainingCardsInLaneWithRowIndexes,
        placeCardAttempt,
        playerIsHost
      )
    );

    return new List<List<CardMovement>> { cardMovements };
  }

  private List<CardMovement> GetCapturedCardMovementsGoingToTheDeck(
    List<(Card, int)> cardsWithRowIndexes,
    PlaceCardAttempt placeCardAttempt,
    bool playerIsHost
  )
  {
    var cardMovements = new List<CardMovement>();

    foreach (var (card, rowIndex) in cardsWithRowIndexes)
    {
      var from = new CardStore()
      {
        CardPosition = new CardPosition(placeCardAttempt.TargetLaneIndex, rowIndex)
      };

      var to = new CardStore() { HostDeck = playerIsHost, GuestDeck = !playerIsHost };

      cardMovements.Add(new CardMovement(from, to, card));
    }

    return cardMovements;
  }

  private List<CardMovement> GetCardMovementsGoingToTheMiddle(
    List<(Card, int)> cardsAndRowIndexes,
    PlaceCardAttempt placeCardAttempt
  )
  {
    var cardMovements = new List<CardMovement>();

    foreach (var (card, rowIndex) in cardsAndRowIndexes)
    {
      var to = new CardStore
      {
        CardPosition = new CardPosition(placeCardAttempt.TargetLaneIndex, 3)
      };

      var from = new CardStore
      {
        CardPosition = new CardPosition(placeCardAttempt.TargetLaneIndex, rowIndex)
      };

      cardMovements.Add(new CardMovement(from, to, card));
    }

    return cardMovements;
  }

  /// <returns>Cards alongside their row indexes.</returns>
  private List<(Card, int)> GrabTopCardsOfFirstThreeRows(Lane lane, bool playerIsHost)
  {
    List<(Card, int)> topCardsOfFirstThreeRows = [];

    int startRow = playerIsHost ? 0 : 6;
    int endRow = playerIsHost ? 3 : 4;
    int step = playerIsHost ? 1 : -1;

    for (int i = startRow; playerIsHost ? i < endRow : i >= endRow; i += step)
    {
      var row = lane.Rows[i];

      if (row.Count > 0)
      {
        var card = row.Last();
        row.RemoveAt(row.Count - 1);

        topCardsOfFirstThreeRows.Add((card, i));
      }
    }

    return topCardsOfFirstThreeRows;
  }

  private List<CardMovement> CaptureNoAdvantageLane(
    Lane lane,
    PlaceCardAttempt placeCardAttempt,
    bool playerIsHost
  )
  {
    var advantagePlayer = playerIsHost ? PlayerOrNone.Host : PlayerOrNone.Guest;
    var laneCardsAndRowIndexes = GrabAllCardsFromLane(lane)
      .OrderBy(cardAndRowIndex => cardAndRowIndex.Item1.PlayedBy == advantagePlayer)
      .ThenBy(cardAndRowIndex => playerIsHost ? cardAndRowIndex.Item2 : -cardAndRowIndex.Item2)
      .ToList();

    var laneCards = laneCardsAndRowIndexes.Select(cardAndRowIndex => cardAndRowIndex.Item1);
    var middleRow = lane.Rows[3];
    middleRow.AddRange(laneCards);
    lane.LaneAdvantage = playerIsHost ? PlayerOrNone.Host : PlayerOrNone.Guest;

    return GetCardMovementsGoingToTheMiddle(laneCardsAndRowIndexes, placeCardAttempt);
  }

  /// <returns>Cards alongside their row indexes ordered from the bottom to the top of the row's stack of cards.</returns>
  private List<(Card, int)> GrabAllCardsFromLane(Lane lane)
  {
    List<(Card, int)> cardsAndRowIndexes = new();

    for (var rowIndex = 0; rowIndex < lane.Rows.Length; rowIndex++)
    {
      var row = lane.Rows[rowIndex];

      foreach (var card in row)
      {
        cardsAndRowIndexes.Add((card, rowIndex));
      }
    }

    lane.Rows = CreateEmptyRows();

    return cardsAndRowIndexes;
  }

  private CardMovement PlaceCard(PlaceCardAttempt placeCardAttempt, bool playerIsHost)
  {
    var lane = Lanes[placeCardAttempt.TargetLaneIndex];
    var currentPlayedBy = playerIsHost ? PlayerOrNone.Host : PlayerOrNone.Guest;
    var targetRow = lane.Rows[placeCardAttempt.TargetRowIndex];
    var topCard = targetRow.LastOrDefault();
    var cardReinforced = topCard is not null && topCard.PlayedBy == currentPlayedBy;
    var mostOffensiveCard = GetMostOffensiveCard(lane, playerIsHost);
    var isCardMostOffensive =
      mostOffensiveCard is not null
      && topCard is not null
      && mostOffensiveCard.SuitAndKindMatch(topCard);

    lane.LastCardPlayed =
      !isCardMostOffensive && cardReinforced ? mostOffensiveCard : placeCardAttempt.Card;

    placeCardAttempt.Card.PlayedBy = currentPlayedBy;
    targetRow.Add(placeCardAttempt.Card);

    var player = playerIsHost ? HostPlayer : GuestPlayer;
    var indexInHand = RemoveCardWithMatchingKindAndSuit(player.Hand.Cards, placeCardAttempt.Card);

    if (indexInHand is null)
    {
      throw new Exception("Attempted to place a card that a player did not have.");
    }

    var from = new CardStore
    {
      HostHandCardIndex = playerIsHost ? indexInHand : null,
      GuestHandCardIndex = playerIsHost ? null : indexInHand
    };

    var to = new CardStore
    {
      CardPosition = new CardPosition(
        placeCardAttempt.TargetLaneIndex,
        placeCardAttempt.TargetRowIndex
      )
    };

    var notation = GetCardMovementNotation(placeCardAttempt);

    return new CardMovement(from, to, placeCardAttempt.Card, notation);
  }

  public string GetCardMovementNotation(PlaceCardAttempt placeCardAttempt)
  {
    var kindLetter = GetKindNotationLetter(placeCardAttempt.Card.Kind);
    var suitLetter = GetSuitNotationLetter(placeCardAttempt.Card.Suit);
    var laneLetter = GetLaneNotationLetter(placeCardAttempt.TargetLaneIndex);
    return $"{kindLetter}{suitLetter}{laneLetter}{placeCardAttempt.TargetRowIndex + 1}";
  }

  public string GetKindNotationLetter(Kind kind)
  {
    return kind switch
    {
      Kind.Ace => "A",
      Kind.Two => "2",
      Kind.Three => "3",
      Kind.Four => "4",
      Kind.Five => "5",
      Kind.Six => "6",
      Kind.Seven => "7",
      Kind.Eight => "8",
      Kind.Nine => "9",
      Kind.Ten => "T",
      Kind.Jack => "J",
      Kind.Queen => "Q",
      Kind.King => "K",
      _ => "",
    };
  }

  public string GetSuitNotationLetter(Suit suit)
  {
    return suit switch
    {
      Suit.Clubs => "♣",
      Suit.Diamonds => "♦",
      Suit.Hearts => "♥",
      Suit.Spades => "♠",
      _ => "",
    };
  }

  public string GetLaneNotationLetter(int laneIndex)
  {
    return laneIndex switch
    {
      0 => "a",
      1 => "b",
      2 => "c",
      3 => "d",
      4 => "e",
      _ => "",
    };
  }

  public int? RemoveCardWithMatchingKindAndSuit(List<Card> cardList, Card card)
  {
    for (int i = 0; i < cardList.Count; i++)
    {
      var cardFromList = cardList[i];
      bool sameSuit = cardFromList.Suit.Equals(card.Suit);
      bool sameKind = cardFromList.Kind.Equals(card.Kind);

      if (sameSuit && sameKind)
      {
        cardList.RemoveAt(i);
        return i;
      }
    }

    return null;
  }

  private List<CardMovement> TriggerAceRuleIfAppropriate(
    PlaceCardAttempt placeCardAttempt,
    bool playerIsHost
  )
  {
    var playerPlayedAnAce = placeCardAttempt.Card.Kind == Kind.Ace;
    if (!playerPlayedAnAce)
    {
      return [];
    }

    var laneIndex = placeCardAttempt.TargetLaneIndex;
    var lane = Lanes[laneIndex];
    var playerAceIsFacingOpponentAce = IsPlayerAceFacingOpponentAce(lane, playerIsHost);

    if (!playerAceIsFacingOpponentAce)
    {
      return [];
    }

    lane.LastCardPlayed = null;
    lane.LaneAdvantage = PlayerOrNone.None;
    var destroyedCardsAndRowIndexes = GrabAllCardsFromLane(lane);

    return GetCardMovementsFromDestroyedCards(destroyedCardsAndRowIndexes, laneIndex);
  }

  public List<CardMovement> GetCardMovementsFromDestroyedCards(
    List<(Card, int)> destroyedCardsAndRowIndexes,
    int laneIndex
  )
  {
    var cardMovements = new List<CardMovement>();

    foreach (var (destroyedCard, rowIndex) in destroyedCardsAndRowIndexes)
    {
      var from = new CardStore { CardPosition = new CardPosition(laneIndex, rowIndex) };

      var to = new CardStore { Destroyed = true };

      cardMovements.Add(new CardMovement(from, to, destroyedCard));
    }

    return cardMovements;
  }

  public List<CardMovement> WinLaneAndOrGameIfAppropriate(
    Game game,
    PlaceCardAttempt placeCardAttempt,
    bool playerIsHost
  )
  {
    var placeCardInLastRow = playerIsHost
      ? placeCardAttempt.TargetRowIndex == 6
      : placeCardAttempt.TargetRowIndex == 0;

    if (!placeCardInLastRow)
    {
      return [];
    }

    game.Lanes[placeCardAttempt.TargetLaneIndex].WonBy = playerIsHost
      ? PlayerOrNone.Host
      : PlayerOrNone.Guest;

    var lane = game.Lanes[placeCardAttempt.TargetLaneIndex];
    var allCardsInLaneWithRowIndexes = GrabAllCardsFromLane(lane);
    var allCardsInLane = allCardsInLaneWithRowIndexes
      .Select(cardWithRowIndex => cardWithRowIndex.Item1)
      .ToList();

    var player = playerIsHost ? game.HostPlayer : game.GuestPlayer;
    player.Deck.Cards.AddRange(allCardsInLane);
    player.Deck.Shuffle();

    if (game.RedJokerLaneIndex is null)
    {
      game.RedJokerLaneIndex = placeCardAttempt.TargetLaneIndex;
    }
    else
    {
      game.BlackJokerLaneIndex = placeCardAttempt.TargetLaneIndex;
    }

    WinGameIfAppropriate();

    return GetCardMovementsFromWonCards(
      allCardsInLaneWithRowIndexes,
      placeCardAttempt,
      playerIsHost
    );
  }

  public List<CardMovement> GetCardMovementsFromWonCards(
    List<(Card, int)> cardsWithRowIndexes,
    PlaceCardAttempt placeCardAttempt,
    bool playerIsHost
  )
  {
    var cardMovements = new List<CardMovement>();

    foreach (var (card, rowIndex) in cardsWithRowIndexes)
    {
      var from = new CardStore()
      {
        CardPosition = new CardPosition(placeCardAttempt.TargetLaneIndex, rowIndex)
      };

      var to = new CardStore() { HostDeck = playerIsHost, GuestDeck = !playerIsHost };

      cardMovements.Add(new CardMovement(from, to, card));
    }

    return cardMovements;
  }

  public bool IsPlayerAceFacingOpponentAce(Lane lane, bool playerIsHost)
  {
    var playersMostOffensiveCard = GetMostOffensiveCard(lane, playerIsHost);
    var opponentsMostOffensiveCard = GetMostOffensiveCard(lane, !playerIsHost);
    var cardsAreAces =
      playersMostOffensiveCard is not null
      && playersMostOffensiveCard.Kind == Kind.Ace
      && opponentsMostOffensiveCard is not null
      && opponentsMostOffensiveCard.Kind == Kind.Ace;

    return cardsAreAces || TopTwoCardsInLaneAreOpposingAces(lane);
  }

  private Card? GetMostOffensiveCard(Lane lane, bool forHostPlayer)
  {
    var mostToLeastOffensive = forHostPlayer ? lane.Rows.Reverse() : lane.Rows;

    foreach (var row in mostToLeastOffensive)
    {
      if (row is null)
      {
        continue;
      }

      var topCard = row.LastOrDefault();
      if (topCard is null)
      {
        continue;
      }

      if (topCard.PlayedBy == (forHostPlayer ? PlayerOrNone.Host : PlayerOrNone.Guest))
      {
        return topCard;
      }
    }

    return null;
  }

  private bool TopTwoCardsInLaneAreOpposingAces(Lane lane)
  {
    foreach (var row in lane.Rows)
    {
      if (row.Count < 2)
      {
        continue;
      }

      var topCard = row[^1];
      var secondTopCard = row[^2];
      if (topCard is null || secondTopCard is null)
      {
        continue;
      }

      if (topCard.Kind == Kind.Ace && secondTopCard.Kind == Kind.Ace)
      {
        return true;
      }
    }

    return false;
  }

  public void Resign(string connectionId)
  {
    End(HostConnectionId == connectionId ? PlayerOrNone.Guest : PlayerOrNone.Host);
  }

  public (Hand, List<RearrangeHandResults>) RearrangeHand(string connectionId, List<Card> cards)
  {
    var playerIsHost = HostConnectionId == connectionId;
    var existingHand = playerIsHost ? HostPlayer.Hand : GuestPlayer.Hand;
    var existingCards = existingHand.Cards;
    bool containsDifferentCards = existingCards.ContainsDifferentCards(cards);
    if (containsDifferentCards)
    {
      return (existingHand, [RearrangeHandResults.InvalidCards]);
    }

    existingHand.Cards = cards;
    return (existingHand, []);
  }

  public void EndByDisconnection()
  {
    End(HostPlayerDisconnectedTimestampUTC is null ? PlayerOrNone.Host : PlayerOrNone.Guest);
  }

  public void EndByClockExpired()
  {
    End(IsHostPlayersTurn ? PlayerOrNone.Guest : PlayerOrNone.Host);
  }

  public List<AcceptDrawOfferResults> AcceptDrawOffer(string connectionId)
  {
    var isHost = HostConnectionId == connectionId;
    var hasOfferToAccept = (isHost && DrawOfferFromGuest) || (!isHost && DrawOfferFromHost);
    if (!hasOfferToAccept)
    {
      return [AcceptDrawOfferResults.NoOfferToAccept];
    }

    DrawOfferFromGuest = false;
    DrawOfferFromHost = false;

    End();

    return [];
  }

  public List<SendChatMessageResults> SendChatMessage(string connectionId, string rawMessage)
  {
    if (rawMessage.Trim().Length == 0)
    {
      return [SendChatMessageResults.MessageHasNoContent];
    }

    var sensoredMessage = rawMessage.ReplaceBadWordsWithAsterisks();
    var sentBy = connectionId == HostConnectionId ? PlayerOrNone.Host : PlayerOrNone.Guest;
    var sentAt = DateTime.UtcNow;
    var chatMessage = new ChatMessage(rawMessage, sensoredMessage, sentAt, sentBy);
    var chatMessageView = new ChatMessageView(sensoredMessage, sentBy, sentAt);
    ChatMessages.Add(chatMessage);
    ChatMessageViews.Add(chatMessageView);

    return [];
  }

  public void MarkLatestReadChatMessageIndex(string connectionId, int latestIndex)
  {
    var isHost = connectionId == HostConnectionId;
    if (isHost)
    {
      HostsLatestReadChatMessageIndex = latestIndex;
    }
    else
    {
      GuestsLatestReadChatMessageIndex = latestIndex;
    }
  }
}
