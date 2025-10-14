# Chess of Cards - Local Test Server

A local WebSocket server for testing the Chess of Cards API without deploying to AWS. This server runs your Lambda functions locally and connects to your dev DynamoDB tables.

## What It Does

- Runs a WebSocket server on `ws://localhost:3001`
- Accepts WebSocket connections from your Angular client
- Invokes Lambda functions (ConnectionHandler and GameActionHandler) directly
- Uses your dev DynamoDB tables for data storage
- Provides real-time logging of all WebSocket activity

## Prerequisites

- .NET 8 SDK
- AWS credentials configured (for DynamoDB access)
- Dev DynamoDB tables must exist in AWS

## Running the Server

### Option 1: Using dotnet run

```bash
cd src/ChessOfCards.LocalTestServer
dotnet run
```

### Option 2: Using Visual Studio / Rider

1. Open the solution in your IDE
2. Set `ChessOfCards.LocalTestServer` as the startup project
3. Press F5 or click Run

### Option 3: Using dotnet watch (auto-reload on code changes)

```bash
cd src/ChessOfCards.LocalTestServer
dotnet watch run
```

## Testing the Server

Once the server is running, you should see:

```
Starting Chess of Cards Local Test Server...
WebSocket URL: ws://localhost:3001
Health Check: http://localhost:3001/health
Environment: dev
```

### Health Check

Visit http://localhost:3001/health to verify the server is running:

```bash
curl http://localhost:3001/health
```

### Server Info

Visit http://localhost:3001/info for server information:

```bash
curl http://localhost:3001/info
```

### Connect with wscat

Test the WebSocket connection manually:

```bash
npm install -g wscat
wscat -c ws://localhost:3001

# Once connected, send a message:
{"action":"createPendingGame","data":{"hostName":"Test Host","durationOption":"ThreeMinutes"}}
```

### Connect with Your Angular App

Update your Angular environment to point to the local server:

```typescript
export const environment = {
  production: false,
  serverUrl: 'http://localhost:3001',
};
```

## How It Works

1. **WebSocket Connection**: Client connects to `ws://localhost:3001`
2. **$connect Route**: Server invokes `ConnectionHandlerFunction` with `$connect` route
3. **Message Handling**: Client sends messages → Server routes to `GameActionHandlerFunction`
4. **$disconnect Route**: Client disconnects → Server invokes `ConnectionHandlerFunction` with `$disconnect` route

## Environment Variables

The server automatically sets these environment variables:

- `ENVIRONMENT_NAME`: `dev`
- `CONNECTIONS_TABLE_NAME`: `chess-of-cards-connections-dev`
- `PENDING_GAMES_TABLE_NAME`: `chess-of-cards-pending-games-dev`
- `ACTIVE_GAMES_TABLE_NAME`: `chess-of-cards-active-games-dev`
- `GAME_TIMERS_TABLE_NAME`: `chess-of-cards-game-timers-dev`
- `WEBSOCKET_ENDPOINT`: `http://localhost:3001`

## Debugging

The server logs all WebSocket activity:

- Connection established/closed
- Messages received from clients
- Lambda function invocations
- Errors and exceptions

Watch the console output for detailed logs.

## Limitations

- WebSocket messages sent from Lambda functions use the local WebSocketManager instead of API Gateway Management API
- Some AWS-specific features may behave differently than in production
- No authentication/authorization (connects directly)

## Benefits

- **Fast Iteration**: Make code changes and test immediately without deploying
- **Real-time Debugging**: See all logs in your console
- **Cost Savings**: No Lambda invocation costs during development
- **Offline Development**: Work without internet (except DynamoDB access)

## Troubleshooting

### Port 3001 already in use

Change the port in `Program.cs`:

```csharp
app.Run("http://localhost:YOUR_PORT");
```

And update your client's `serverUrl` accordingly.

### Cannot connect to DynamoDB

Ensure your AWS credentials are configured:

```bash
aws configure
```

Verify your dev tables exist:

```bash
aws dynamodb list-tables --region us-east-1
```

### Lambda function errors

Check the console logs for detailed error messages. The server logs all Lambda function invocations and responses.
