# Serverless Architecture Design - Chess of Cards API

## Overview

This document outlines the serverless architecture for migrating the Chess of Cards game from an EC2-based WebSocket server to AWS serverless services.

## Current (Legacy) Architecture

### Components
- **API Layer**: SignalR Hub on EC2 with WebSocket support
- **State Management**: In-memory repositories (GameRepository, PendingGameRepository)
- **Business Logic**: MediatR command/handler pattern
- **Timers**: Background timer service for game clocks and disconnect handling
- **Connection Management**: SignalR connection tracking

### Limitations
- Requires persistent EC2 instance
- State lost on server restart
- Horizontal scaling challenges
- Manual server management

## Proposed Serverless Architecture

### High-Level Components

```
┌─────────────┐
│   Client    │
│ (WebSocket) │
└──────┬──────┘
       │
       ▼
┌─────────────────────────────────┐
│  API Gateway WebSocket API      │
│  - $connect                     │
│  - $disconnect                  │
│  - Custom routes (game actions) │
└────────┬────────────────────────┘
         │
         ▼
┌──────────────────────────────────┐
│     Lambda Functions             │
│  - ConnectionHandler             │
│  - GameActionHandler             │
│  - TimerHandler                  │
└────────┬─────────────────────────┘
         │
         ▼
┌──────────────────────────────────┐
│        DynamoDB Tables           │
│  - Connections                   │
│  - PendingGames                  │
│  - ActiveGames                   │
│  - GameTimers                    │
└──────────────────────────────────┘
         ▲
         │
┌────────┴─────────────────────────┐
│  EventBridge + Step Functions    │
│  - Game clock timers             │
│  - Disconnect grace periods      │
└──────────────────────────────────┘
```

## DynamoDB Table Design

### 1. Connections Table

**Purpose**: Track active WebSocket connections and map them to players/games.

**Schema**:
```
PK: connectionId (String) - Partition Key
SK: "CONNECTION" (String) - Sort Key

Attributes:
- gameCode (String) - Game the player is in
- playerRole (String) - "HOST" or "GUEST"
- playerName (String) - Display name
- connectedAt (Number) - Unix timestamp
- ttl (Number) - Auto-expire after 24 hours
```

**Access Patterns**:
- Find connection by connectionId: `GetItem(PK=connectionId)`
- Find all connections for a game: GSI on gameCode

**GSI**: ConnectionsByGame
- PK: gameCode
- SK: connectionId

### 2. PendingGames Table

**Purpose**: Store games waiting for a second player to join.

**Schema**:
```
PK: gameCode (String) - Partition Key
SK: "PENDING" (String) - Sort Key

Attributes:
- hostConnectionId (String)
- hostName (String)
- durationOption (String) - "SHORT", "MEDIUM", "LONG"
- createdAt (Number) - Unix timestamp
- ttl (Number) - Auto-expire after 10 minutes
```

**Access Patterns**:
- Find pending game by code: `GetItem(PK=gameCode)`
- List all pending games: `Scan` (for lobby display)
- Find pending game by host connection: GSI on hostConnectionId

**GSI**: PendingGamesByHost
- PK: hostConnectionId
- SK: createdAt

### 3. ActiveGames Table

**Purpose**: Store full game state for active games.

**Schema**:
```
PK: gameCode (String) - Partition Key
SK: "GAME" (String) - Sort Key

Attributes:
- hostConnectionId (String)
- guestConnectionId (String)
- hostName (String)
- guestName (String)
- gameState (String) - JSON serialized Game object
- isHostPlayersTurn (Boolean)
- hasEnded (Boolean)
- wonBy (String) - "HOST", "GUEST", "NONE"
- durationOption (String)
- createdAt (Number)
- updatedAt (Number)
- version (Number) - Optimistic locking
- hostDisconnectedAt (Number, nullable)
- guestDisconnectedAt (Number, nullable)
- ttl (Number) - Auto-expire after 7 days
```

**Access Patterns**:
- Find game by code: `GetItem(PK=gameCode)`
- Find game by connection: GSI on hostConnectionId/guestConnectionId

**GSI**: GamesByConnection
- PK: hostConnectionId (or composite of both)
- SK: createdAt

**Note**: The full `gameState` JSON contains the serialized Game domain object including:
- Players (decks, hands)
- Lanes with cards
- Move history
- Chat messages
- Candidate moves

### 4. GameTimers Table

**Purpose**: Track game clocks and disconnect timers with automatic expiry.

**Schema**:
```
PK: timerId (String) - Partition Key (format: "GAME#{gameCode}#CLOCK" or "DISCONNECT#{gameCode}#{role}")
SK: "TIMER" (String) - Sort Key

Attributes:
- gameCode (String)
- timerType (String) - "GAME_CLOCK_HOST", "GAME_CLOCK_GUEST", "DISCONNECT"
- playerRole (String) - "HOST" or "GUEST" (for disconnect timers)
- expiresAt (Number) - Unix timestamp
- startedAt (Number)
- pausedAt (Number, nullable)
- secondsElapsed (Number)
- secondsRemaining (Number)
- ttl (Number) - DynamoDB TTL for auto-cleanup
```

**Access Patterns**:
- Find timer by ID: `GetItem(PK=timerId)`
- Find all timers for game: Begins with query
- Process expired timers: DynamoDB Streams

**GSI**: TimersByExpiry
- PK: timerType
- SK: expiresAt (for efficient polling)

## Lambda Functions

### 1. ConnectionHandler

**Trigger**: API Gateway WebSocket $connect and $disconnect routes

**Responsibilities**:
- **$connect**:
  - Create connection record in Connections table
  - Check for reconnection scenarios
  - Return success/failure

- **$disconnect**:
  - Mark player as disconnected in ActiveGames
  - Start disconnect timer (30 seconds grace period)
  - Notify opponent via WebSocket
  - Delete connection record after grace period

**Environment Variables**:
- CONNECTIONS_TABLE_NAME
- ACTIVE_GAMES_TABLE_NAME
- GAME_TIMERS_TABLE_NAME

### 2. GameActionHandler

**Trigger**: API Gateway WebSocket custom routes

**Routes**:
- `createPendingGame` - Create new game lobby
- `deletePendingGame` - Cancel game lobby
- `joinGame` - Join existing game by code
- `makeMove` - Play cards
- `passMove` - Skip turn
- `resignGame` - Forfeit game
- `offerDraw` - Propose draw
- `acceptDrawOffer` - Accept draw
- `sendChatMessage` - Send chat
- `markLatestReadChatMessage` - Update read status
- `rearrangeHand` - Reorder cards in hand

**Responsibilities**:
1. Parse incoming WebSocket message
2. Load game state from DynamoDB
3. Validate action using domain logic
4. Update game state
5. Save updated state to DynamoDB (with optimistic locking)
6. Broadcast updates to both players
7. Manage game timers

**Environment Variables**:
- CONNECTIONS_TABLE_NAME
- PENDING_GAMES_TABLE_NAME
- ACTIVE_GAMES_TABLE_NAME
- GAME_TIMERS_TABLE_NAME
- WEBSOCKET_ENDPOINT (for @connections API)

### 3. TimerHandler

**Trigger**: EventBridge scheduled events (every 1 second) OR DynamoDB Streams on GameTimers table

**Responsibilities**:
- Query timers approaching expiry (next 2 seconds)
- For each expired timer:
  - **Game Clock**: End game, mark opponent as winner
  - **Disconnect Timer**: End game if player hasn't reconnected
- Update game state in ActiveGames
- Notify connected players
- Clean up timer records

**Alternative Approach**: Use Step Functions with Wait states for precise timing

**Environment Variables**:
- GAME_TIMERS_TABLE_NAME
- ACTIVE_GAMES_TABLE_NAME
- CONNECTIONS_TABLE_NAME
- WEBSOCKET_ENDPOINT

### 4. GameStartHandler (Optional)

**Trigger**: DynamoDB Stream on PendingGames table (when deleted after successful join)

**Responsibilities**:
- Initialize game state
- Create ActiveGame record
- Start game clocks
- Notify both players

## WebSocket Message Protocol

### Client → Server Messages

Format:
```json
{
  "action": "makeMove",
  "data": {
    "move": { ... },
    "rearrangedCardsInHand": [ ... ]
  }
}
```

Actions:
- `createPendingGame`: `{ durationOption, hostName }`
- `joinGame`: `{ gameCode, guestName }`
- `makeMove`: `{ move, rearrangedCardsInHand }`
- `passMove`: `{}`
- `resignGame`: `{}`
- `offerDraw`: `{}`
- `acceptDrawOffer`: `{}`
- `sendChatMessage`: `{ rawMessage }`
- `markLatestReadChatMessage`: `{ latestIndex }`
- `rearrangeHand`: `{ cards }`
- `deletePendingGame`: `{}`

### Server → Client Messages

Format:
```json
{
  "type": "GameUpdated",
  "data": {
    "game": { ... }
  }
}
```

Message Types (mapped from legacy command names):
- `CreatedPendingGame`: `{ pendingGame }`
- `GameStarted`: `{ game }`
- `GameUpdated`: `{ game }`
- `GameOver`: `{ game, reason }`
- `OpponentDisconnected`: `{ playerRole }`
- `OpponentReconnected`: `{ playerRole }`
- `PlayerReconnected`: `{ game }`
- `ChatMessageSent`: `{ chatMessageView }`
- `DrawOffered`: `{ byPlayerRole }`
- `TurnSkipped`: `{ skippedPlayerRole }`
- `JoinGameCodeInvalid`: `{}`
- `GameNameInvalid`: `{}`
- `LatestReadChatMessageMarked`: `{ playerRole, latestIndex }`

## Timer Management Strategy

### Option 1: EventBridge + Polling (Recommended for MVP)

**Approach**:
- EventBridge rule triggers Lambda every 1-2 seconds
- Lambda queries GameTimers table for timers expiring soon
- Process expired timers

**Pros**:
- Simple implementation
- Easy to debug
- Predictable cost

**Cons**:
- Not sub-second precision
- Always running (small background cost)

### Option 2: Step Functions with Wait States

**Approach**:
- Start Step Function when game begins
- Use Wait state until timer expires
- Execute Lambda when time runs out

**Pros**:
- Precise timing
- No polling overhead
- Pay per execution

**Cons**:
- More complex
- Step Function state machine management
- Max execution time: 1 year (sufficient for chess games)

### Option 3: DynamoDB TTL + Streams

**Approach**:
- Set TTL on timer records
- Lambda triggered by DynamoDB Streams when item deleted
- Process game end

**Pros**:
- No polling
- Automatic cleanup
- Minimal cost

**Cons**:
- TTL is not precise (can be up to 48 hours delayed, typically minutes)
- Not suitable for real-time game clocks

### Recommended Hybrid Approach

Use **Option 1** (EventBridge polling) for game clocks during active play:
- Precision: 1-2 seconds is acceptable
- Simple to implement
- Query: `expiresAt < now + 2 seconds`

Use **Option 3** (DynamoDB TTL) for cleanup:
- Auto-delete old games after 7 days
- Auto-delete connections after 24 hours
- Auto-delete abandoned pending games after 10 minutes

## Connection Management

### Broadcasting Messages

To send a message to a specific connection:

```csharp
var client = new AmazonApiGatewayManagementApiClient(
    new AmazonApiGatewayManagementApiConfig
    {
        ServiceURL = websocketEndpoint
    }
);

await client.PostToConnectionAsync(new PostToConnectionRequest
{
    ConnectionId = connectionId,
    Data = new MemoryStream(Encoding.UTF8.GetBytes(jsonMessage))
});
```

### Broadcasting to Both Players

1. Load game from ActiveGames table
2. Get both connectionIds
3. Serialize appropriate game view for each player (hide opponent's hand)
4. Post to each connection
5. Handle stale connections (GoneException)

### Handling Disconnections

**Graceful Disconnect Flow**:
1. Client disconnects → $disconnect triggered
2. Lambda marks player as disconnected in ActiveGames
3. Lambda creates disconnect timer (30 seconds)
4. Lambda notifies opponent: "Player disconnected"
5. Timer expires:
   - If player reconnected: Do nothing
   - If player still disconnected: End game, opponent wins

**Reconnection Flow**:
1. Client connects → $connect triggered
2. Lambda checks for game with disconnected player matching name/criteria
3. If found:
   - Update connectionId in ActiveGames
   - Clear disconnect timer
   - Notify opponent: "Player reconnected"
   - Send full game state to reconnected player

## Optimistic Locking Strategy

To prevent race conditions when multiple Lambda invocations try to update the same game:

**Approach**:
1. Add `version` attribute to ActiveGames records
2. When loading game: Store version number
3. When updating game: Use conditional write
   ```csharp
   UpdateItemRequest {
     ConditionExpression = "version = :expectedVersion",
     ExpressionAttributeValues = {
       ":expectedVersion": currentVersion,
       ":newVersion": currentVersion + 1
     }
   }
   ```
4. If condition fails: Retry (load latest, re-apply logic, save)

## Cost Estimation

### DynamoDB

Assuming 1000 concurrent games:

**Storage**:
- Connections: 2000 records × 1 KB = 2 MB
- PendingGames: 100 records × 1 KB = 0.1 MB
- ActiveGames: 1000 records × 50 KB = 50 MB
- GameTimers: 3000 records × 1 KB = 3 MB
- **Total**: ~55 MB → **$0.28/month**

**Read/Write**:
- Moves per game: 100
- Games per day: 1000
- Total operations: 100K reads + 100K writes
- **Cost**: ~$0.125 per day → **$3.75/month**

### Lambda

**Invocations**:
- Game actions: 100K/day
- Timer checks: 86,400/day (every second)
- Connections: 2K/day
- **Total**: ~190K/day → **5.7M/month**

With 512 MB memory, 1s average duration:
- Compute: 5.7M × 1s × $0.0000166667 = **$95/month**

### API Gateway

**WebSocket Messages**:
- Connections: 2K/day
- Messages: 200K/day (100K in + 100K out)
- **Total**: ~6M/month → **$6/month**

### Total Estimated Cost

- **Development/Low Traffic**: $5-10/month
- **Production (1000 daily games)**: $100-120/month
- **Scaling**: Costs scale linearly with usage

Compare to EC2 t3.medium: $30-50/month (always running) + limited scalability

## Migration Strategy

### Phase 1: Core Infrastructure
1. Create DynamoDB tables
2. Implement ConnectionHandler Lambda
3. Set up API Gateway WebSocket API
4. Test basic connect/disconnect

### Phase 2: Game Lobby
1. Implement createPendingGame
2. Implement joinGame
3. Implement deletePendingGame
4. Test game creation flow

### Phase 3: Core Gameplay
1. Port Game domain logic
2. Implement makeMove
3. Implement passMove
4. Test game mechanics

### Phase 4: Additional Features
1. Implement resignGame
2. Implement draw offers
3. Implement chat
4. Test complete game flow

### Phase 5: Timer System
1. Implement game clock tracking
2. Add EventBridge rule
3. Implement TimerHandler
4. Test timer expiry

### Phase 6: Polish
1. Reconnection handling
2. Error handling improvements
3. Logging and monitoring
4. Performance optimization

## Testing Strategy

### Unit Tests
- Domain logic (Game class methods)
- Message serialization/deserialization
- Timer calculations

### Integration Tests
- DynamoDB operations
- WebSocket message flow
- Lambda invocations

### End-to-End Tests
- Complete game flow
- Reconnection scenarios
- Timer expiry
- Error conditions

### Load Tests
- Concurrent games
- Message throughput
- Lambda cold starts

## Monitoring and Observability

### CloudWatch Metrics
- Lambda invocation counts
- Lambda errors
- Lambda duration
- DynamoDB throttling
- API Gateway errors

### CloudWatch Logs
- Structured JSON logging
- Request/response payloads
- Game state changes
- Timer events

### X-Ray Tracing
- End-to-end request tracing
- Performance bottlenecks
- DynamoDB call latency

### Custom Metrics
- Active games count
- Average game duration
- Move frequency
- Player reconnection rate

## Security Considerations

### Authentication
- Consider adding Cognito for user authentication
- Store userId in connection records
- Validate user owns the game they're trying to modify

### Authorization
- Verify connectionId matches player in game
- Prevent spectators from sending commands
- Rate limiting on game actions

### Data Privacy
- Don't log sensitive player data
- Sanitize chat messages
- Implement profanity filtering

### DDoS Protection
- API Gateway throttling limits
- WAF rules for WebSocket endpoint
- DynamoDB auto-scaling

## Future Enhancements

### Observability Dashboard
- Real-time game statistics
- Player analytics
- System health metrics

### Matchmaking
- Queue system for random opponents
- ELO rating system
- Skill-based matching

### Replay System
- Store move history separately
- Replay viewer
- Game analysis

### Tournament Support
- Multi-game brackets
- Leaderboards
- Prize tracking

### Mobile Push Notifications
- Notify when opponent moves
- Game start notifications
- Tournament updates

## Conclusion

This serverless architecture provides:
- **Scalability**: Automatic scaling based on demand
- **Reliability**: No single point of failure
- **Cost Efficiency**: Pay only for actual usage
- **Maintainability**: No server management

The migration can be done incrementally, with each phase building on the previous one. The legacy domain logic can be largely reused, with the main changes being in state persistence and connection management.
