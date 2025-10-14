# Chess of Cards API

A serverless WebSocket-based multiplayer game server for Chess of Cards, built with .NET 8, AWS Lambda, API Gateway WebSocket API, and DynamoDB.

## Overview

This project implements a real-time multiplayer card game server using AWS serverless technologies. Players connect via WebSocket to create games, join lobbies, and play Chess of Cards with persistent state management in DynamoDB.

## Architecture

- **API Gateway WebSocket API** - Manages WebSocket connections and routes messages
- **AWS Lambda Functions** - Serverless compute for connection handling and game logic
- **DynamoDB** - NoSQL database for connection tracking, game state, and timers
- **AWS SAM** - Infrastructure as Code for deployment and local testing

### Lambda Functions

1. **ConnectionHandler** - Manages WebSocket lifecycle (`$connect`, `$disconnect`)
2. **GameActionHandler** - Processes game actions (create, join, move, etc.)

### DynamoDB Tables

1. **ConnectionsTable** - Active WebSocket connections
2. **PendingGamesTable** - Game lobbies awaiting players
3. **ActiveGamesTable** - Active game state
4. **GameTimersTable** - Game clocks and disconnect timers

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [AWS SAM CLI](https://docs.aws.amazon.com/serverless-application-model/latest/developerguide/serverless-sam-cli-install.html)
- [Docker](https://www.docker.com/products/docker-desktop) (for SAM local testing)
- [AWS CLI](https://aws.amazon.com/cli/) (configured with credentials)

## Getting Started

### Quick Start

```bash
# Install dependencies (one time)
npm install

# Start local server
npm start

# Or start with auto-reload
npm run start:watch
```

Server will be available at `http://localhost:5000` (WebSocket: `ws://localhost:5000/ws`)

### Build the Application

```bash
# Build .NET solution
npm run build
# or
dotnet build ChessOfCards.sln

# Build SAM packages
npm run build:sam
# or
sam build

# Build with Docker (for Lambda compatibility)
npm run build:sam:container
# or
sam build --use-container --mount-with WRITE
```

### Local Development

```bash
# Option 1: Custom local server (fastest)
npm start

# Option 2: SAM local API
npm run sam:local
# or
sam local start-api

# Test with wscat
npm install -g wscat
wscat -c ws://localhost:5000

# Send a test message
{"action": "createPendingGame", "playerName": "Alice"}
```

### Deploy to AWS

```bash
# First time deployment (guided)
sam deploy --guided

# Deploy to dev
npm run sam:deploy:dev
# or
sam deploy --parameter-overrides Environment=dev

# Deploy to prod
npm run sam:deploy:prod
# or
sam deploy --parameter-overrides Environment=prod
```

### View Logs

```bash
# Tail logs for ConnectionHandler
npm run sam:logs:connection
# or
sam logs -n ConnectionHandlerFunction --stack-name ChessOfCardsApi-Dev --tail

# Tail logs for GameActionHandler
npm run sam:logs:game
# or
sam logs -n GameActionHandlerFunction --stack-name ChessOfCardsApi-Dev --tail
```

### Other Commands

```bash
# Run tests
npm test

# Validate SAM template
npm run sam:validate

# Format code
npm run format
```

## Project Structure

```
chess-of-cards-api/
├── src/
│   ├── ChessOfCards.Infrastructure/          # Shared library (models, repos, services)
│   ├── ChessOfCards.ConnectionHandler/       # WebSocket lifecycle Lambda
│   ├── ChessOfCards.GameActionHandler/       # Game actions Lambda
│   ├── ChessOfCards.GameActionHandler.Application/  # Game logic
│   └── ChessOfCards.Shared.Utilities/        # Shared utilities
├── legacy/                                    # Original EC2-based implementation
├── template.yaml                              # SAM infrastructure template
├── samconfig.toml                            # SAM CLI configuration
├── ChessOfCards.sln                          # .NET solution file
├── CLAUDE.md                                 # Development guidance
├── PROJECT_STATUS.md                         # Project roadmap
└── README.md                                 # This file
```

## Game Flow

### 1. Create a Game Lobby

```json
{
  "action": "createPendingGame",
  "playerName": "Alice"
}
```

**Response:**
```json
{
  "MessageType": "PendingGameCreated",
  "GameCode": "ABC123",
  "HostName": "Alice"
}
```

### 2. Join a Game

```json
{
  "action": "joinGame",
  "gameCode": "ABC123",
  "playerName": "Bob"
}
```

**Response (to both players):**
```json
{
  "MessageType": "GameStarted",
  "GameCode": "ABC123",
  "HostName": "Alice",
  "GuestName": "Bob",
  "GameState": "{...}"
}
```

### 3. Delete Pending Game

```json
{
  "action": "deletePendingGame",
  "gameCode": "ABC123"
}
```

## CI/CD Pipeline

The project uses GitHub Actions for automated deployment:

- **Push to `develop`** → Deploys to Dev environment (`ChessOfCardsApi-Dev`)
- **Push to `main`** → Deploys to Production environment (`ChessOfCardsApi-Prod`)

### Pipeline Stages

1. **Build** - Compiles .NET projects and creates SAM artifacts
2. **Deploy Dev** - Deploys to development (conditional on `develop` branch)
3. **Deploy Prod** - Deploys to production (conditional on `main` branch)

### GitHub Secrets Required

- `AWS_ACCESS_KEY_ID` - AWS access key (per environment)
- `AWS_SECRET_ACCESS_KEY` - AWS secret key (per environment)

## Development Status

### Completed ✅
- WebSocket API infrastructure
- Connection lifecycle management
- Game lobby system (create, join, delete)
- DynamoDB repositories and models
- WebSocket messaging service
- CI/CD pipeline

### In Progress 🔄
- Core gameplay logic (Phase 3)
- Game state management
- Move validation and processing

### Planned ⏳
- Timer system for game clocks
- Reconnection logic
- Comprehensive unit and integration tests
- Authentication with AWS Cognito (optional)

See [docs/PROJECT_STATUS.md](docs/PROJECT_STATUS.md) for detailed roadmap.

## Configuration

### Environment Variables (Lambda)

- `ENVIRONMENT_NAME` - Environment identifier (dev/prod)
- `CONNECTIONS_TABLE_NAME` - ConnectionsTable name
- `PENDING_GAMES_TABLE_NAME` - PendingGamesTable name
- `ACTIVE_GAMES_TABLE_NAME` - ActiveGamesTable name
- `GAME_TIMERS_TABLE_NAME` - GameTimersTable name
- `WEBSOCKET_ENDPOINT` - API Gateway WebSocket endpoint URL

### Parameters (SAM Template)

- `Environment` - Environment name (default: dev)

## Testing

### Unit Tests

```bash
# Run all tests
dotnet test ChessOfCards.sln

# Run tests for specific project (when available)
dotnet test tests/ChessOfCards.Tests/ChessOfCards.Tests.csproj
```

### Manual Testing with wscat

```bash
# Install wscat globally
npm install -g wscat

# Connect to local WebSocket API
wscat -c ws://localhost:3001

# Connect to deployed WebSocket API
wscat -c wss://your-api-id.execute-api.us-east-1.amazonaws.com/dev
```

## Cleanup

To delete the deployed stack:

```bash
sam delete --stack-name ChessOfCardsApi-Dev
sam delete --stack-name ChessOfCardsApi-Prod
```

## Documentation

- **[Quick Start Guide](docs/QUICK_START.md)** - Get started in 3 steps
- **[Local Testing](docs/LOCAL_TESTING.md)** - Run the server locally
- **[Deployment Guide](docs/DEPLOYMENT.md)** - Deploy to AWS
- **[Architecture](docs/SERVERLESS_ARCHITECTURE.md)** - System design
- **[Project Status](docs/PROJECT_STATUS.md)** - Roadmap and progress
- **[Best Practices](docs/BEST_PRACTICES_TODO.md)** - Optimization checklist

## Resources

- [AWS SAM Documentation](https://docs.aws.amazon.com/serverless-application-model/)
- [API Gateway WebSocket API](https://docs.aws.amazon.com/apigateway/latest/developerguide/apigateway-websocket-api.html)
- [DynamoDB Best Practices](https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/best-practices.html)
- [Lambda Best Practices](https://docs.aws.amazon.com/lambda/latest/dg/best-practices.html)

## License

[Include your license information here]

## Contributing

[Include contribution guidelines here]
