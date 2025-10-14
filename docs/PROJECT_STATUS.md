# Chess of Cards API - Project Status & Roadmap

## Project Context

**Goal**: Migrate the Chess of Cards game from an EC2-based WebSocket server (using SignalR) to a fully serverless AWS architecture using API Gateway WebSocket, Lambda, and DynamoDB.

**Legacy System**:
- Location: `legacy/` folder
- Architecture: ASP.NET Core + SignalR on EC2
- State: In-memory repositories
- Limitations: Manual scaling, no persistence, single point of failure

**New System**:
- Architecture: API Gateway WebSocket + Lambda + DynamoDB
- Benefits: Auto-scaling, persistent state, cost-efficient, no server management
- Status: **Phase 1 & 2 Complete** - Infrastructure and Lobby System operational

## Current Progress

### ✅ Phase 1: Core Infrastructure (COMPLETE)

**Completed:**
- [x] SAM template with WebSocket API and DynamoDB tables
- [x] Infrastructure shared library (ChessOfCards.Infrastructure)
  - [x] DynamoDB entity models (ConnectionRecord, PendingGameRecord, ActiveGameRecord, GameTimerRecord)
  - [x] Repository interfaces and implementations
  - [x] WebSocketService for messaging
  - [x] Message type definitions
- [x] ConnectionHandler Lambda
  - [x] $connect route - Create connection records
  - [x] $disconnect route - Handle disconnections with grace period
  - [x] Disconnect timer creation
  - [x] Opponent notifications
- [x] Build verification - All projects compile successfully
- [x] Security fixes - Updated System.Text.Json to 8.0.5

**Files Created:**
- `template.yaml` - Complete SAM infrastructure
- `src/ChessOfCards.Infrastructure/` - Shared library
- `src/ChessOfCards.ConnectionHandler/` - Connection lifecycle Lambda
- `SERVERLESS_ARCHITECTURE.md` - Detailed design document
- `DEPLOYMENT.md` - Deployment guide

### ✅ Phase 2: Game Lobby (COMPLETE)

**Completed:**
- [x] GameActionHandler Lambda project
- [x] Action routing system
- [x] createPendingGame action
  - [x] Game code generation (6-char alphanumeric)
  - [x] Name validation and profanity filtering
  - [x] DynamoDB pending game creation
  - [x] Connection record updates
  - [x] Client notifications
- [x] joinGame action
  - [x] Game code validation
  - [x] Pending game lookup
  - [x] Active game creation
  - [x] Both player notifications
  - [x] Connection updates
- [x] deletePendingGame action
  - [x] Host-only deletion
  - [x] Cleanup of pending games
- [x] Build verification - GameActionHandler compiles

**Files Created:**
- `src/ChessOfCards.GameActionHandler/` - Game actions Lambda

**What Works:**
- Players can connect to WebSocket
- Host can create a game lobby
- Guest can join with game code
- Both players receive "GameStarted" notification
- Host can cancel pending games
- Invalid actions are handled gracefully

## Current State

**Deployable**: Yes! Ready for `sam build && sam deploy`

**Testable**: Yes! Can test lobby flow with wscat or browser client

**Production Ready**: No - Core gameplay not yet implemented

## Remaining Work

### 🔄 Phase 3: Core Gameplay (IN PROGRESS)

**Next Tasks:**
1. Port Game domain logic from `legacy/ChessOfCards.Domain/`
   - Copy `Game.cs` (1600+ lines of game rules)
   - Copy card entities (Card, Suit, Kind)
   - Copy player entities (Player, Deck, Hand)
   - Copy move entities (Move, PlaceCardAttempt, CandidateMove)
   - Copy lane entities (Lane)
   - Adapt for JSON serialization to/from DynamoDB

2. Implement game initialization
   - Create initial game state when game starts
   - Shuffle and deal cards
   - Generate candidate moves
   - Serialize to JSON for DynamoDB storage

3. Implement makeMove action
   - Deserialize game state from DynamoDB
   - Validate move using Game domain logic
   - Update game state
   - Serialize back to DynamoDB (with optimistic locking)
   - Broadcast updated game view to both players
   - Handle turn switching

4. Implement passMove action
   - Skip turn logic
   - Draw cards until hand has 5
   - Check for three consecutive passes (game end)
   - Update game state

5. Implement resignGame action
   - Mark game as ended
   - Set winner as opponent
   - Broadcast game over notification

**Estimated Effort**: 2-3 sessions
- Domain logic porting: 1 session
- Integration & testing: 1-2 sessions

### 📋 Phase 4: Additional Features

**Tasks:**
1. Implement offerDraw action
   - Track draw offers in game state
   - Notify opponent

2. Implement acceptDrawOffer action
   - Validate offer exists
   - End game as draw

3. Implement sendChatMessage action
   - Add chat messages to game state
   - Broadcast to both players
   - Profanity filtering

4. Implement markLatestReadChatMessage action
   - Track read status per player

5. Implement rearrangeHand action
   - Update card order in player's hand

**Estimated Effort**: 1-2 sessions

### ⏱️ Phase 5: Timer System

**Tasks:**
1. Implement TimerHandler Lambda
   - Query timers expiring in next 60 seconds
   - Process game clock expirations
   - Process disconnect timer expirations
   - Update games and notify players

2. Implement game clock management
   - Start clocks when game begins
   - Pause/resume on turn changes
   - Track elapsed time
   - Handle time expiration (opponent wins)

3. Test timer accuracy
   - EventBridge scheduling precision
   - Race condition handling

**Estimated Effort**: 1 session

### 🔒 Phase 6: Authentication & Polish

**Tasks:**
1. Add Cognito authentication (optional)
   - User pools
   - JWT token validation
   - User identity in connection records

2. Implement reconnection logic
   - Identify returning players
   - Restore game state
   - Clear disconnect timers

3. Add proper profanity filter
   - Replace placeholder bad words list
   - Use established filtering library

4. Optimize DynamoDB queries
   - Add necessary GSIs
   - Batch operations where possible

5. Add comprehensive logging
   - Structured logging
   - Performance metrics
   - Error tracking

**Estimated Effort**: 2-3 sessions

### 🧪 Phase 7: Testing & Documentation

**Tasks:**
1. Unit tests
   - Repository tests
   - Game logic tests
   - Message serialization tests

2. Integration tests
   - WebSocket flow tests
   - End-to-end game tests
   - Error scenario tests

3. Load tests
   - Concurrent games
   - Message throughput
   - Lambda cold start optimization

4. Documentation
   - API documentation
   - Client integration guide
   - Deployment runbook

**Estimated Effort**: 2-3 sessions

## Technical Debt & Known Issues

### Minor Issues
1. ConnectionHandler has 1 async warning (non-blocking)
2. Reconnection logic is stubbed out (awaiting auth implementation)
3. Bad words filter uses placeholder list
4. Game state is currently empty JSON "{}" (awaiting domain logic)

### Future Enhancements
1. Matchmaking system
2. ELO rating
3. Game replay/history
4. Tournament support
5. Spectator mode
6. Mobile push notifications
7. Analytics dashboard

## File Structure

```
chess-of-cards-api/
├── legacy/                          # Original EC2 implementation
│   ├── ChessOfCards.Api/           # SignalR Hub
│   ├── ChessOfCards.Application/   # MediatR handlers
│   ├── ChessOfCards.DataAccess/    # In-memory repositories
│   └── ChessOfCards.Domain/        # Game logic (to be ported)
├── src/
│   ├── ChessOfCards.Infrastructure/     # ✅ Shared library
│   │   ├── Models/                      # DynamoDB entities
│   │   ├── Repositories/                # Data access
│   │   ├── Services/                    # WebSocket service
│   │   └── Messages/                    # Message types
│   ├── ChessOfCards.ConnectionHandler/  # ✅ WebSocket lifecycle
│   ├── ChessOfCards.GameActionHandler/  # ✅ Game actions
│   ├── ChessOfCards.TimerHandler/       # ⏳ TODO: Timer processing
│   └── ServerlessAPI/                   # 🗑️ Old REST API (to be removed)
├── tests/
│   └── ServerlessAPI.Tests/        # 🔄 TODO: Update for new architecture
├── template.yaml                   # ✅ SAM infrastructure
├── SERVERLESS_ARCHITECTURE.md     # ✅ Architecture design
├── DEPLOYMENT.md                   # ✅ Deployment guide
├── CLAUDE.md                       # ✅ Development guide
├── PROJECT_STATUS.md              # ✅ This file
└── README.md                       # 🔄 TODO: Update

Legend: ✅ Complete | 🔄 In Progress | ⏳ TODO | 🗑️ To Remove
```

## Key Decisions & Rationale

### Why API Gateway WebSocket over AppSync?
- More control over connection lifecycle
- Direct Lambda integration
- Cost-effective for this use case
- No GraphQL overhead

### Why Pay-Per-Request DynamoDB?
- Unpredictable traffic patterns
- Development-friendly (no capacity planning)
- Cost-effective for low-medium traffic
- Can switch to provisioned later if needed

### Why EventBridge polling over Step Functions?
- Simpler implementation for MVP
- Easier debugging
- Sufficient precision (1-2 seconds)
- Lower cost for this use case

### Why JSON serialization for game state?
- Flexible schema evolution
- Easy debugging (readable in DynamoDB console)
- No complex serialization logic
- Trade-off: Slightly larger storage (acceptable)

### Why optimistic locking?
- Prevents race conditions (two moves at once)
- Safer than last-write-wins
- Small retry overhead acceptable

## Success Metrics

### MVP Success Criteria
- [x] WebSocket connections stable
- [x] Lobby creation/joining works
- [ ] Complete game can be played
- [ ] Timers expire correctly
- [ ] No data loss on disconnections
- [ ] Sub-second latency for moves

### Production Readiness Criteria
- [ ] 99.9% uptime
- [ ] <500ms p99 latency
- [ ] Handles 100+ concurrent games
- [ ] Reconnection works reliably
- [ ] Comprehensive error handling
- [ ] CloudWatch dashboards
- [ ] Automated deployments
- [ ] Cost under $200/month for 1000 daily games

## Next Session Priorities

**Immediate (Phase 3 Start):**
1. Copy Game domain entities from legacy to Infrastructure project
2. Test JSON serialization/deserialization of game state
3. Implement game initialization on joinGame
4. Implement basic makeMove action

**Quick Wins:**
- Update README.md with new architecture overview
- Add more detailed CloudWatch logging
- Create simple test client HTML page

**Blockers**: None currently

## Resources

- [AWS SAM Documentation](https://docs.aws.amazon.com/serverless-application-model/)
- [API Gateway WebSocket API](https://docs.aws.amazon.com/apigateway/latest/developerguide/apigateway-websocket-api.html)
- [DynamoDB Best Practices](https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/best-practices.html)
- [Lambda Best Practices](https://docs.aws.amazon.com/lambda/latest/dg/best-practices.html)

---

**Last Updated**: 2025-10-13
**Current Phase**: Phase 3 - Core Gameplay (Not Started)
**Next Milestone**: Complete game can be played end-to-end
