# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a .NET 8 serverless application deployed to AWS Lambda using SAM (Serverless Application Model). The application is a WebSocket-based multiplayer game server for Chess of Cards, using API Gateway WebSocket API, Lambda functions, and DynamoDB for state management.

## Technology Stack

- **Runtime**: .NET 8 / C#
- **Framework**: AWS Lambda with WebSocket support
- **AWS Services**: Lambda, API Gateway (WebSocket API), DynamoDB
- **Infrastructure**: AWS SAM for deployment
- **Testing**: xUnit (in development)

## Essential Commands

### Building and Testing

```bash
# Build the SAM application
sam build

# Build with Docker container (required for Lambda compatibility)
sam build --use-container --mount-with WRITE

# Build the solution
dotnet build ChessOfCards.sln
```

### Local Development

```bash
# Start WebSocket API locally
sam local start-api

# Test with wscat (WebSocket client)
npm install -g wscat
wscat -c ws://localhost:3001
```

### Deployment

```bash
# Deploy with guided prompts (first time)
sam deploy --guided

# Deploy with existing config (uses samconfig.toml)
sam deploy

# Deploy to specific environment (dev or prod)
sam deploy --parameter-overrides Environment=dev
sam deploy --parameter-overrides Environment=prod

# View logs from deployed Lambda functions
sam logs -n ConnectionHandlerFunction --stack-name ChessOfCardsApi-Dev --tail
sam logs -n GameActionHandlerFunction --stack-name ChessOfCardsApi-Dev --tail
```

### Validation

```bash
# Validate SAM template with linting
sam validate --lint
```

## Architecture

### Application Structure

```
src/
├── ChessOfCards.Infrastructure/          # Shared library
│   ├── Models/                          # DynamoDB entity models
│   │   ├── ConnectionRecord.cs
│   │   ├── PendingGameRecord.cs
│   │   ├── ActiveGameRecord.cs
│   │   └── GameTimerRecord.cs
│   ├── Repositories/                    # Data access layer
│   │   ├── IConnectionRepository.cs
│   │   ├── ConnectionRepository.cs
│   │   ├── IPendingGameRepository.cs
│   │   ├── PendingGameRepository.cs
│   │   ├── IActiveGameRepository.cs
│   │   ├── ActiveGameRepository.cs
│   │   ├── IGameTimerRepository.cs
│   │   └── GameTimerRepository.cs
│   ├── Services/                        # Business logic
│   │   └── WebSocketService.cs
│   └── Messages/                        # Message type definitions
│       └── MessageTypes.cs
├── ChessOfCards.ConnectionHandler/      # WebSocket lifecycle Lambda
│   ├── Function.cs                      # Handles $connect, $disconnect
│   └── ChessOfCards.ConnectionHandler.csproj
├── ChessOfCards.GameActionHandler/      # Game actions Lambda
│   ├── Function.cs                      # Routes game actions
│   ├── Handlers/                        # Action-specific handlers
│   └── ChessOfCards.GameActionHandler.csproj
├── ChessOfCards.GameActionHandler.Application/  # Game logic
│   └── ChessOfCards.GameActionHandler.Application.csproj
└── ChessOfCards.Shared.Utilities/       # Shared utilities
    └── ChessOfCards.Shared.Utilities.csproj
```

### Lambda Functions

#### ConnectionHandler
- **Purpose**: Manages WebSocket connection lifecycle
- **Routes**: `$connect`, `$disconnect`
- **Responsibilities**:
  - Create connection records in DynamoDB
  - Handle graceful disconnections with grace period
  - Notify opponents of disconnections
  - Clean up timers

#### GameActionHandler
- **Purpose**: Processes all game actions
- **Route**: `$default` (all non-connection messages)
- **Actions**:
  - `createPendingGame` - Host creates a game lobby
  - `joinGame` - Guest joins with game code
  - `deletePendingGame` - Host cancels pending game
  - `makeMove` - Player makes a move (in development)
  - `passMove` - Player passes their turn (in development)
  - `resignGame` - Player resigns (in development)

### DynamoDB Tables

#### ConnectionsTable
- **Purpose**: Track active WebSocket connections
- **Primary Key**: `connectionId` (String)
- **GSI**: `GameCodeIndex` (gameCode)
- **TTL**: Enabled for automatic cleanup

#### PendingGamesTable
- **Purpose**: Store game lobbies awaiting second player
- **Primary Key**: `gameCode` (String)
- **GSI**: `HostConnectionIndex` (hostConnectionId + createdAt)
- **TTL**: Enabled for automatic cleanup

#### ActiveGamesTable
- **Purpose**: Store active game state
- **Primary Key**: `gameCode` (String)
- **GSIs**:
  - `HostConnectionIndex` (hostConnectionId + createdAt)
  - `GuestConnectionIndex` (guestConnectionId + createdAt)
- **TTL**: Enabled for automatic cleanup

#### GameTimersTable
- **Purpose**: Track game clocks and disconnect timers
- **Primary Key**: `timerId` (String)
- **GSI**: `ExpiryIndex` (timerType + expiresAt)
- **TTL**: Enabled for automatic cleanup

## Infrastructure (template.yaml)

### Key Resources

- **ChessWebSocketApi** - API Gateway WebSocket API
  - Protocol: WEBSOCKET
  - Route Selection: `$request.body.action`
  - Stages: dev, prod

- **ConnectionHandlerFunction** - Connection lifecycle Lambda
  - Runtime: dotnet8
  - Handler: ChessOfCards.ConnectionHandler::ChessOfCards.ConnectionHandler.Function::FunctionHandler
  - Memory: 512 MB
  - Timeout: 30 seconds

- **GameActionHandlerFunction** - Game actions Lambda
  - Runtime: dotnet8
  - Handler: ChessOfCards.GameActionHandler::ChessOfCards.GameActionHandler.Function::FunctionHandler
  - Memory: 1024 MB
  - Timeout: 60 seconds

### Environment Parameters

The stack supports `Environment` parameter (dev/prod) which:
- Determines all DynamoDB table name suffixes
- Is passed to Lambda via `ENVIRONMENT_NAME` environment variable
- Controls WebSocket API stage name
- Defaults to "dev"

### Build Configuration

- Uses `Makefile` build method for multi-project builds
- Shared `Makefile` in `src/` directory handles compilation of all projects
- Produces Lambda deployment packages for each function

## CI/CD Pipeline (.github/workflows/main.yml)

### Workflow Stages

1. **Build Job** - Runs on all pushes to main/develop
   - Builds SAM application with Docker
   - Uploads artifacts for deployment jobs

2. **Deploy Dev** - Runs only on pushes to `develop` branch
   - Deploys to dev environment with `Environment=dev`
   - Uses GitHub environment: `dev`
   - Stack name: `ChessOfCardsApi-Dev`

3. **Deploy Prod** - Runs only on pushes to `main` branch
   - Deploys to production with `Environment=prod`
   - Uses GitHub environment: `prod`
   - Stack name: `ChessOfCardsApi-Prod`

### Important Notes

- SAM build uses `--mount-with WRITE` flag to avoid interactive prompts in CI
- AWS credentials are stored as environment-specific secrets
- Deploys to `us-east-1` region

## Message Protocol

### Client-to-Server Messages

All messages are JSON with an `action` field:

```json
{
  "action": "createPendingGame",
  "playerName": "Alice"
}
```

```json
{
  "action": "joinGame",
  "gameCode": "ABC123",
  "playerName": "Bob"
}
```

### Server-to-Client Messages

Messages use `MessageType` field for routing:

```json
{
  "MessageType": "GameStarted",
  "GameCode": "ABC123",
  "HostName": "Alice",
  "GuestName": "Bob",
  "GameState": "{...}"
}
```

## Configuration Files

- **samconfig.toml** - SAM CLI configuration with build/deploy defaults
- **omnisharp.json** - C# IDE configuration
- **Makefile** (in src/) - Build orchestration for multiple Lambda projects

## Current Development Status

### Completed (Phase 1 & 2)
- ✅ WebSocket API infrastructure
- ✅ Connection lifecycle management
- ✅ Game lobby system (create, join, delete)
- ✅ DynamoDB repositories
- ✅ WebSocket messaging service
- ✅ CI/CD pipeline

### In Progress (Phase 3)
- 🔄 Core gameplay logic
- 🔄 Game state management
- 🔄 Move validation

### Planned
- ⏳ Timer system for game clocks
- ⏳ Reconnection logic
- ⏳ Comprehensive testing
- ⏳ Authentication (optional)

## Important Implementation Notes

### WebSocket Connection Flow

1. Client connects → `$connect` route → ConnectionHandler creates record
2. Client sends action → `$default` route → GameActionHandler processes
3. Client disconnects → `$disconnect` route → ConnectionHandler cleanup

### Error Handling

- All Lambda functions return proper WebSocket response format
- Errors are logged to CloudWatch with structured JSON
- Clients receive error messages via WebSocket

### Logging

- Uses structured JSON logging via AWS Lambda logging
- All operations log to CloudWatch Logs
- Log groups: `/aws/lambda/ConnectionHandlerFunction`, `/aws/lambda/GameActionHandlerFunction`

## AWS Region

Default deployment region is `us-east-1` (N. Virginia). All resources are deployed to this region by default.

## Related Documentation

- **PROJECT_STATUS.md** - Detailed project roadmap and current progress
- **SERVERLESS_ARCHITECTURE.md** - Architecture design document
- **DEPLOYMENT.md** - Deployment guide
- **README.md** - Project overview
