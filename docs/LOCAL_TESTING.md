# Local Testing Guide

This guide explains how to test your Chess of Cards API locally without deploying to AWS.

## Overview

The `ChessOfCards.LocalTestServer` project provides a local WebSocket server that:

- ✅ Runs your Lambda functions directly (no Docker or SAM required)
- ✅ Connects to your dev DynamoDB tables in AWS
- ✅ Provides real-time logging and debugging
- ✅ Supports hot-reload with `dotnet watch`
- ✅ Allows rapid iteration without deployment delays

## Quick Start

### 1. Ensure Prerequisites

- AWS credentials configured (`aws configure`)
- Dev DynamoDB tables exist in AWS
- .NET 8 SDK installed

### 2. Start the Server

**Option A: Using npm (easiest)**
```bash
npm start                 # Start on default port (5000)
npm run start:watch       # Start with auto-reload
npm run start:rebuild     # Rebuild first, then start
npm run start:8080        # Start on port 8080
```

**Option B: Using VS Code Task**
1. Press `Ctrl+Shift+P` (Windows/Linux) or `Cmd+Shift+P` (Mac)
2. Type "Run Task"
3. Select "Run Local Test Server"

**Option C: Using PowerShell script**
```powershell
# From project root
.\scripts\run-local-server.ps1

# With custom port
.\scripts\run-local-server.ps1 -Port 8080

# Rebuild solution first
.\scripts\run-local-server.ps1 -Rebuild

# Auto-reload on file changes
.\scripts\run-local-server.ps1 -Watch
```

**Option D: Using dotnet CLI**
```bash
cd tools/ChessOfCards.LocalTestServer
dotnet run
```

**Option E: Using dotnet watch (auto-reload on changes)**
```bash
cd tools/ChessOfCards.LocalTestServer
dotnet watch run
```

### 3. Update Your Angular App

Point your Angular app to the local server:

```typescript
// src/environments/environment.ts
export const environment = {
  production: false,
  serverUrl: 'http://localhost:3001',  // Note: http not https!
};
```

The WebSocket service will automatically convert this to `ws://localhost:3001`.

### 4. Start Testing!

Open your Angular app and it will connect to the local server. All WebSocket traffic will be logged to the console.

## Server Endpoints

| Endpoint | Type | Description |
|----------|------|-------------|
| `ws://localhost:3001` | WebSocket | Main WebSocket endpoint |
| `http://localhost:3001/health` | HTTP GET | Health check |
| `http://localhost:3001/info` | HTTP GET | Server information |

## Testing with wscat

You can also test directly with wscat:

```bash
# Install wscat
npm install -g wscat

# Connect to the server
wscat -c ws://localhost:3001

# Send a message (once connected)
{"action":"createPendingGame","data":{"hostName":"Alice","durationOption":"ThreeMinutes"}}
```

## How It Works

```
┌──────────────────┐
│  Angular Client  │
└────────┬─────────┘
         │ ws://localhost:3001
         ↓
┌──────────────────────────────────┐
│  ChessOfCards.LocalTestServer    │
│  (ASP.NET Core WebSocket Server) │
└────────┬─────────────────────────┘
         │
         ├─→ ConnectionHandler.Function ($connect, $disconnect)
         │
         ├─→ GameActionHandler.Function (all game actions)
         │
         └─→ AWS DynamoDB (dev tables)
```

### Connection Flow

1. **Client connects** → Server creates connection ID → Invokes `ConnectionHandler` with `$connect`
2. **Client sends message** → Server parses action → Invokes `GameActionHandler` with message
3. **Client disconnects** → Server invokes `ConnectionHandler` with `$disconnect`

### Message Flow

All messages use the same format as production:

**Client → Server:**
```json
{
  "action": "createPendingGame",
  "data": {
    "hostName": "Alice",
    "durationOption": "ThreeMinutes"
  }
}
```

**Server → Client:**
```json
{
  "type": "CreatedPendingGame",
  "data": {
    "gameCode": "ABCD",
    "durationOption": "ThreeMinutes",
    "hostName": "Alice"
  }
}
```

## Debugging Tips

### View All Logs

The server logs everything to the console:
- Connection events
- Incoming messages
- Lambda function invocations
- Outgoing messages
- Errors and exceptions

### Common Issues

**"Port 3001 already in use"**

Change the port in `Program.cs`:
```csharp
app.Run("http://localhost:YOUR_PORT");
```

**"Cannot connect to DynamoDB"**

Check your AWS credentials:
```bash
aws sts get-caller-identity
```

Verify tables exist:
```bash
aws dynamodb list-tables --region us-east-1 | grep chess-of-cards
```

**"Lambda function not found"**

Make sure you've built the solution:
```bash
dotnet build
```

## Development Workflow

### Making Changes

1. Make code changes to your Lambda functions
2. The local server will automatically reload (if using `dotnet watch`)
3. Test immediately - no deployment needed!

### Typical Development Cycle

```bash
# Terminal 1: Run the local server with auto-reload
cd src/ChessOfCards.LocalTestServer
dotnet watch run

# Terminal 2: Run your Angular app
cd ../your-angular-app
ng serve

# Make changes to Lambda code → Server auto-reloads → Test in browser
```

## Environment Variables

The server automatically configures these for local testing:

```bash
ENVIRONMENT_NAME=dev
CONNECTIONS_TABLE_NAME=chess-of-cards-connections-dev
PENDING_GAMES_TABLE_NAME=chess-of-cards-pending-games-dev
ACTIVE_GAMES_TABLE_NAME=chess-of-cards-active-games-dev
GAME_TIMERS_TABLE_NAME=chess-of-cards-game-timers-dev
WEBSOCKET_ENDPOINT=http://localhost:3001
```

## Comparison: Local vs SAM Local vs Deployed

| Feature | Local Test Server | SAM Local | Deployed (Dev) |
|---------|------------------|-----------|----------------|
| Setup Time | < 5 seconds | ~30 seconds | ~5 minutes |
| Hot Reload | ✅ Yes | ❌ No | ❌ No |
| Debugging | ✅ Easy | ⚠️ Complex | ❌ CloudWatch only |
| Docker Required | ❌ No | ✅ Yes | N/A |
| Cost | $0 (DynamoDB only) | $0 | $ Lambda + API GW |
| Internet Required | AWS access only | AWS access only | ✅ Yes |
| WebSocket Support | ✅ Full | ❌ Limited | ✅ Full |

## Limitations

- Uses in-memory WebSocket connections (not API Gateway Management API)
- No authentication/authorization
- Some AWS-specific behaviors may differ slightly
- Requires AWS credentials for DynamoDB access

## Benefits

- ⚡ **Fast**: Start testing in seconds
- 🔄 **Auto-reload**: Code changes reflect immediately with `dotnet watch`
- 🐛 **Easy debugging**: Full console logs and breakpoint support
- 💰 **Cost-effective**: No Lambda invocation costs
- 🚀 **Productive**: Iterate quickly without deployment delays

## VS Code Tasks

This project includes several useful VS Code tasks. Access them via:
- **Quick Access**: `Ctrl+Shift+P` → "Run Task"
- **Keyboard Shortcut**: `Ctrl+Shift+B` (default build task)

Available tasks:
- **Run Local Test Server** - Start the local WebSocket server
- **Run Local Test Server (Rebuild First)** - Rebuild and start server
- **Build Solution** - Build the entire solution
- **Run Tests** - Run all xUnit tests
- **SAM Build** - Build Lambda packages with SAM
- **SAM Local Start API** - Start SAM local API
- **SAM Deploy (Dev)** - Deploy to dev environment

## Next Steps

- Start the server: `npm start` (or `npm run start:watch` for auto-reload)
- Alternative: Press `Ctrl+Shift+P` → "Run Task" → "Run Local Test Server"
- Point your Angular app to `http://localhost:5000` (default port)
- Make changes and test immediately!
- Deploy to dev when ready: `npm run build:sam:container && npm run sam:deploy:dev`

For more details, see `tools/ChessOfCards.LocalTestServer/README.md`
