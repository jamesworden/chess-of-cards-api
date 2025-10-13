# Deployment Checklist

## Pre-Deployment ✓

- [x] All code builds successfully
  - `dotnet build src/ChessOfCards.Infrastructure`
  - `dotnet build src/ChessOfCards.ConnectionHandler`
  - `dotnet build src/ChessOfCards.GameActionHandler`
- [x] SAM template validated (syntax)
- [x] AWS credentials configured
- [x] .NET 8 SDK installed
- [ ] SAM CLI installed and working
- [ ] Docker running (for `sam build --use-container`)

## Deployment Commands

### 1. Build
```bash
sam build --use-container
```
**Expected:** "Build Succeeded" message

### 2. Deploy
```bash
sam deploy --guided
```

**Use these settings:**
- Stack Name: `chess-of-cards-api-dev`
- AWS Region: `us-east-2` (or your preferred region)
- Parameter Environment: `dev`
- Confirm changes: `Y`
- Allow IAM role creation: `Y`
- Disable rollback: `N`
- Save config: `Y`

### 3. Save Outputs
After deployment, copy the WebSocket endpoint:
```
WebSocketApiEndpoint: wss://xxxxxx.execute-api.us-east-2.amazonaws.com/dev
```

## Post-Deployment Verification

### Quick Smoke Tests

#### Test 1: Connect
```bash
wscat -c wss://YOUR-ENDPOINT-HERE/dev
```
Expected: `{"type":"Connected","data":{...}}`

#### Test 2: Create Game
```json
{"action":"createPendingGame","data":{"hostName":"Test","durationOption":"MEDIUM"}}
```
Expected: `{"type":"CreatedPendingGame","data":{"gameCode":"XXXXXX",...}}`

#### Test 3: Check DynamoDB
- Go to AWS Console → DynamoDB
- Check tables exist:
  - `chess-of-cards-connections-dev`
  - `chess-of-cards-pending-games-dev`
  - `chess-of-cards-active-games-dev`
  - `chess-of-cards-game-timers-dev`

#### Test 4: Check Logs
```bash
sam logs -n ConnectionHandlerFunction --stack-name chess-of-cards-api-dev --tail
```

## What to Test

Follow the complete test plan in `TESTING_GUIDE.md`:

1. **Connection Management**
   - [ ] Basic connection
   - [ ] Disconnection cleanup

2. **Game Lobby**
   - [ ] Create pending game
   - [ ] Join game (requires 2 terminals)
   - [ ] Invalid game code handling
   - [ ] Delete pending game
   - [ ] Name validation

3. **Advanced**
   - [ ] Disconnect during active game
   - [ ] Check timers created

## Known Limitations (Current Phase)

- ✅ Lobby creation works
- ✅ Game joining works
- ❌ Cannot play moves yet (Phase 3)
- ❌ Game state is empty JSON (Phase 3)
- ❌ Timer handler not deployed yet (Phase 5) - TimerHandler commented out in template.yaml
- ❌ Reconnection stubbed out (Phase 6)

## Troubleshooting

### Build fails
```bash
# Try without Docker
sam build

# Or check .NET SDK version
dotnet --version
```

### Deploy fails
- Check AWS credentials: `aws sts get-caller-identity`
- Check CloudFormation console for error details
- Verify no conflicting resources exist

### Connection fails
- Verify WebSocket endpoint URL is correct
- Check if API Gateway stage is deployed
- Look at CloudWatch Logs

### Message not processed
- Check GameActionHandler logs
- Verify message format (JSON with "action" field)
- Check DynamoDB for state

## Rollback

If deployment fails or has issues:

```bash
# Delete entire stack
sam delete --stack-name chess-of-cards-api-dev

# Redeploy from scratch
sam build --use-container
sam deploy --guided
```

## Cost Monitoring

After deployment, set up billing alerts:
1. AWS Console → Billing → Budgets
2. Create budget for $10/month
3. Alert at 80% threshold

Expected cost for dev/testing: $1-5/month

## Next Actions

✅ **If all tests pass:**
- Update PROJECT_STATUS.md with deployment info
- Note WebSocket endpoint for future use
- Proceed to Phase 3 (Core Gameplay)

⚠️ **If tests fail:**
- Document issues
- Check CloudWatch Logs
- Review error messages
- Fix and redeploy

---

**Deployment Date:** _____________

**WebSocket Endpoint:** _____________

**Deployed By:** _____________

**Issues Found:** _____________
