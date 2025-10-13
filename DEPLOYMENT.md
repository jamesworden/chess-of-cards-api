# Chess of Cards API - Deployment Guide

## Prerequisites

Before deploying, ensure you have:

1. **AWS CLI** installed and configured
   ```bash
   aws configure
   ```

2. **SAM CLI** installed
   ```bash
   # Windows (via Chocolatey)
   choco install aws-sam-cli

   # macOS
   brew install aws-sam-cli

   # Linux
   pip install aws-sam-cli
   ```

3. **.NET 8 SDK** installed
   ```bash
   dotnet --version  # Should be 8.0.x
   ```

4. **Docker** installed (for `sam build --use-container`)

## Build Status

✅ All projects compile successfully:
- `ChessOfCards.Infrastructure` - Shared library
- `ChessOfCards.ConnectionHandler` - WebSocket connection management
- `ChessOfCards.GameActionHandler` - Game lobby and actions

## Quick Start

### 1. Build the Application

```bash
# Build all Lambda functions with Docker
sam build --use-container

# Or build without Docker (faster, but requires .NET 8 SDK)
sam build
```

### 2. Deploy to Development

```bash
# First time deployment (interactive)
sam deploy --guided

# Follow the prompts:
# - Stack Name: chess-of-cards-api-dev
# - AWS Region: us-east-2 (or your preferred region)
# - Parameter Environment: dev
# - Confirm changes before deploy: Y
# - Allow SAM CLI IAM role creation: Y
# - Disable rollback: N
# - Save arguments to configuration file: Y
```

### 3. Deploy to Production

```bash
sam deploy --parameter-overrides Environment=prod
```

## Deployment Configuration

The deployment is controlled by:

### Parameters (`template.yaml`)

- `Environment`: `dev` or `prod` (default: `dev`)

### Outputs

After deployment, you'll receive:

```
Outputs
-----------------------------------------------------------------
Key                    Value
WebSocketApiEndpoint   wss://xxxxx.execute-api.us-east-2.amazonaws.com/dev
ConnectionsTableName   chess-of-cards-connections-dev
PendingGamesTableName  chess-of-cards-pending-games-dev
ActiveGamesTableName   chess-of-cards-active-games-dev
GameTimersTableName    chess-of-cards-game-timers-dev
```

Save the `WebSocketApiEndpoint` - this is what clients will connect to!

## Architecture Overview

```
Client (WebSocket)
    ↓
API Gateway WebSocket API
    ↓
┌──────────────────────────────────────┐
│  Lambda Functions                    │
│  - ConnectionHandler ($connect/disc) │
│  - GameActionHandler (game actions)  │
│  - TimerHandler (scheduled)          │
└──────────────────────────────────────┘
    ↓
┌──────────────────────────────────────┐
│  DynamoDB Tables                     │
│  - Connections                       │
│  - PendingGames                      │
│  - ActiveGames                       │
│  - GameTimers                        │
└──────────────────────────────────────┘
```

## Testing the Deployment

### Using wscat (WebSocket CLI tool)

```bash
# Install wscat
npm install -g wscat

# Connect to WebSocket API
wscat -c wss://xxxxx.execute-api.us-east-2.amazonaws.com/dev

# Once connected, test creating a game
> {"action":"createPendingGame","data":{"hostName":"Alice","durationOption":"MEDIUM"}}

# You should receive:
< {"type":"CreatedPendingGame","data":{"gameCode":"A3K7N2","durationOption":"MEDIUM","hostName":"Alice"}}
```

### Using a Web Browser Client

```javascript
const ws = new WebSocket('wss://xxxxx.execute-api.us-east-2.amazonaws.com/dev');

ws.onopen = () => {
    console.log('Connected!');

    // Create a game
    ws.send(JSON.stringify({
        action: 'createPendingGame',
        data: {
            hostName: 'TestPlayer',
            durationOption: 'MEDIUM'
        }
    }));
};

ws.onmessage = (event) => {
    const message = JSON.parse(event.data);
    console.log('Received:', message);
};

ws.onerror = (error) => {
    console.error('WebSocket error:', error);
};
```

## Monitoring

### CloudWatch Logs

Lambda function logs are automatically sent to CloudWatch:

```bash
# View ConnectionHandler logs
sam logs -n ConnectionHandlerFunction --stack-name chess-of-cards-api-dev --tail

# View GameActionHandler logs
sam logs -n GameActionHandlerFunction --stack-name chess-of-cards-api-dev --tail

# View TimerHandler logs
sam logs -n TimerHandlerFunction --stack-name chess-of-cards-api-dev --tail
```

### DynamoDB Tables

View table contents in AWS Console:
- DynamoDB → Tables → `chess-of-cards-*-dev`

Or via AWS CLI:

```bash
# Scan connections table
aws dynamodb scan --table-name chess-of-cards-connections-dev

# Get specific game
aws dynamodb get-item \
    --table-name chess-of-cards-active-games-dev \
    --key '{"gameCode": {"S": "A3K7N2"}}'
```

## Cleanup

To delete all resources:

```bash
sam delete --stack-name chess-of-cards-api-dev
```

## Cost Estimation

For development/testing with low traffic:

- **DynamoDB**: Pay-per-request pricing (~$0.25-1/month)
- **Lambda**: Free tier covers most dev usage (~$0-5/month)
- **API Gateway**: $1 per million messages (~$0.01-1/month)
- **CloudWatch Logs**: ~$0.50-2/month

**Total estimated cost for dev**: $1-10/month

For production (1000 games/day):
- See `SERVERLESS_ARCHITECTURE.md` for detailed cost breakdown
- Estimated: $100-120/month

## Troubleshooting

### Build Errors

**Error**: `Unable to find image 'public.ecr.aws/sam/build-dotnet8:latest'`

**Solution**: Make sure Docker is running:
```bash
docker ps
```

### Deployment Errors

**Error**: `CREATE_FAILED: Resource handler returned message: "Endpoint is not connected"`

**Solution**: This is usually transient. Wait a minute and try again.

**Error**: `Table already exists`

**Solution**: Either:
- Delete the existing table in DynamoDB Console
- Use a different stack name
- Use `sam delete` to remove the old stack

### Runtime Errors

**Lambda timeout**: Increase timeout in `template.yaml`

```yaml
Globals:
  Function:
    Timeout: 30  # Increase from default
```

**Out of memory**: Increase memory in `template.yaml`

```yaml
ConnectionHandlerFunction:
  Properties:
    MemorySize: 1024  # Increase from 512
```

## CI/CD Integration

The project includes GitHub Actions workflow (`.github/workflows/main.yml`):

- **Automatic deployment** on push to `main` (prod) or `develop` (dev)
- **Build artifacts** are cached for faster deployments
- **Environment-specific** secrets for AWS credentials

### Setup GitHub Actions

1. Add secrets to your GitHub repository:
   - `AWS_ACCESS_KEY_ID` (in both `dev` and `prod` environments)
   - `AWS_SECRET_ACCESS_KEY` (in both `dev` and `prod` environments)

2. Create S3 bucket for deployment artifacts:
   ```bash
   aws s3 mb s3://chess-of-cards-api-artifacts
   ```

3. Push to `develop` branch → deploys to dev
4. Push to `main` branch → deploys to prod

## Next Steps

Once deployed, you can:

1. **Test the lobby flow** - Create and join games
2. **Implement game logic** - Add move validation, game state
3. **Add authentication** - Cognito integration
4. **Monitor performance** - CloudWatch metrics
5. **Optimize costs** - Reserved capacity for DynamoDB

## Support

For issues or questions:
- Check CloudWatch Logs for errors
- Review DynamoDB table contents
- See `SERVERLESS_ARCHITECTURE.md` for architecture details
- See `CLAUDE.md` for development guidance
