# Chess of Cards API - Testing Guide

## Pre-Deployment Checklist

Before deploying, ensure you have:

- [x] All projects build successfully
- [x] AWS CLI configured with valid credentials
- [ ] SAM CLI installed (`sam --version`)
- [ ] Sufficient AWS permissions (CloudFormation, Lambda, DynamoDB, API Gateway, IAM)
- [ ] S3 bucket for deployment artifacts (or SAM will create one)

## Deployment Steps

### Step 1: Build the Application

```bash
# Recommended: Build with Docker for Lambda compatibility
sam build --use-container

# Alternative: Build locally (faster but requires .NET 8)
sam build
```

**Expected Output:**
```
Build Succeeded

Built Artifacts  : .aws-sam/build
Built Template   : .aws-sam/build/template.yaml
```

### Step 2: Deploy to AWS

```bash
# First time deployment (interactive)
sam deploy --guided
```

**Configuration Prompts:**

```
Setting default arguments for 'sam deploy'
=========================================
Stack Name [chess-of-cards-api]: chess-of-cards-api-dev
AWS Region [us-east-2]: us-east-2
Parameter Environment [dev]: dev
#Shows you resources changes to be deployed and require a 'Y' to initiate deploy
Confirm changes before deploy [Y/n]: Y
#SAM needs permission to be able to create roles to connect to the resources in your template
Allow SAM CLI IAM role creation [Y/n]: Y
#Preserves the state of previously provisioned resources when an operation fails
Disable rollback [y/N]: N
Save arguments to configuration file [Y/n]: Y
SAM configuration file [samconfig.toml]: samconfig.toml
SAM configuration environment [default]: default
```

**Expected Deployment Time:** 3-5 minutes

**What Gets Created:**
- WebSocket API Gateway
- 3 Lambda Functions
- 4 DynamoDB Tables
- IAM Roles and Policies
- CloudWatch Log Groups
- EventBridge Rule

### Step 3: Save Deployment Outputs

After successful deployment, you'll see:

```
CloudFormation outputs from deployed stack
---------------------------------------------------------------------------
Outputs
---------------------------------------------------------------------------
Key                    WebSocketApiEndpoint
Description            WebSocket API endpoint URL
Value                  wss://abc123def.execute-api.us-east-2.amazonaws.com/dev

Key                    ConnectionsTableName
Description            Connections DynamoDB table name
Value                  chess-of-cards-connections-dev
```

**IMPORTANT:** Save the `WebSocketApiEndpoint` value - you'll need it for testing!

## Testing Phase 1: Connection Management

### Test 1.1: Basic Connection

**Using wscat (command line):**

```bash
# Install wscat if needed
npm install -g wscat

# Connect to WebSocket
wscat -c wss://abc123def.execute-api.us-east-2.amazonaws.com/dev
```

**Expected Response:**
```json
< {"type":"Connected","data":{"connectionId":"abc123xyz"}}
```

**Verify:**
- Check DynamoDB Connections table in AWS Console
- Should see a new record with your connectionId

### Test 1.2: Disconnection Handling

**Steps:**
1. Connect with wscat
2. Press Ctrl+C to disconnect
3. Check CloudWatch Logs for ConnectionHandler

**Expected in Logs:**
```
Disconnection: abc123xyz
Deleted connection record for abc123xyz
```

**Verify:**
- Connection record should be deleted from DynamoDB

## Testing Phase 2: Game Lobby

### Test 2.1: Create Pending Game

**Connect and send:**
```json
{"action":"createPendingGame","data":{"hostName":"TestPlayer1","durationOption":"MEDIUM"}}
```

**Expected Response:**
```json
{
  "type":"CreatedPendingGame",
  "data":{
    "gameCode":"A3K7N2",
    "durationOption":"MEDIUM",
    "hostName":"TestPlayer1"
  }
}
```

**Verify in DynamoDB:**
- Check PendingGames table - should see game with generated code
- Check Connections table - connectionId should have gameCode and playerRole="HOST"

### Test 2.2: Join Game (Two Connections Required)

**Terminal 1 (Host):**
```bash
wscat -c wss://abc123def.execute-api.us-east-2.amazonaws.com/dev

# Wait for Connected message, then:
> {"action":"createPendingGame","data":{"hostName":"Alice","durationOption":"MEDIUM"}}

# Note the gameCode in response (e.g., "A3K7N2")
```

**Terminal 2 (Guest):**
```bash
wscat -c wss://abc123def.execute-api.us-east-2.amazonaws.com/dev

# Wait for Connected message, then use the gameCode from Terminal 1:
> {"action":"joinGame","data":{"gameCode":"A3K7N2","guestName":"Bob"}}
```

**Expected Response (Both Terminals):**
```json
{
  "type":"GameStarted",
  "data":{
    "gameCode":"A3K7N2",
    "hostName":"Alice",
    "guestName":"Bob",
    "durationOption":"MEDIUM",
    "isHostPlayersTurn":true
  }
}
```

**Verify in DynamoDB:**
- PendingGames table - game should be DELETED
- ActiveGames table - new game should exist with both players
- Connections table - both connections should have gameCode

### Test 2.3: Invalid Game Code

**Connect and send:**
```json
{"action":"joinGame","data":{"gameCode":"INVALID","guestName":"TestPlayer"}}
```

**Expected Response:**
```json
{
  "type":"JoinGameCodeInvalid",
  "data":{"gameCode":"INVALID"}
}
```

### Test 2.4: Delete Pending Game

**Connect and send:**
```json
{"action":"createPendingGame","data":{"hostName":"HostUser","durationOption":"SHORT"}}
```

**Wait for response, then send:**
```json
{"action":"deletePendingGame","data":{}}
```

**Verify in DynamoDB:**
- PendingGames table - game should be deleted
- Connections table - connectionId should have gameCode=null

### Test 2.5: Name Validation

**Test profanity filter (placeholder currently):**
```json
{"action":"createPendingGame","data":{"hostName":"badword1","durationOption":"MEDIUM"}}
```

**Expected Response:**
```json
{
  "type":"GameNameInvalid",
  "data":{"reason":"Name contains inappropriate content"}
}
```

## Testing Phase 3: Disconnect Handling (Advanced)

### Test 3.1: Disconnect During Active Game

**Setup:**
1. Create game with Host (Terminal 1)
2. Join game with Guest (Terminal 2)
3. Disconnect Guest (Ctrl+C in Terminal 2)

**Expected in Terminal 1:**
```json
{
  "type":"OpponentDisconnected",
  "data":{"playerRole":"GUEST"}
}
```

**Verify in DynamoDB:**
- ActiveGames table - game should have `guestDisconnectedAt` timestamp
- GameTimers table - should see disconnect timer for GUEST

### Test 3.2: Reconnection (Currently Stubbed)

Currently, reconnection logic is not implemented. When a disconnected player reconnects, they get a new connectionId and cannot rejoin their game. This will be implemented in Phase 6.

## Monitoring & Debugging

### CloudWatch Logs

**View Lambda Logs:**
```bash
# ConnectionHandler logs
sam logs -n ConnectionHandlerFunction --stack-name chess-of-cards-api-dev --tail

# GameActionHandler logs
sam logs -n GameActionHandlerFunction --stack-name chess-of-cards-api-dev --tail

# Or in AWS Console:
# CloudWatch > Log Groups > /aws/lambda/chess-of-cards-api-dev-*
```

### DynamoDB Console

**View Table Data:**
1. AWS Console → DynamoDB → Tables
2. Select table (e.g., `chess-of-cards-connections-dev`)
3. Click "Explore table items"

**Useful Queries:**
- All connections: Scan `Connections` table
- Pending games: Scan `PendingGames` table
- Active games: Scan `ActiveGames` table
- Specific game: Get item with gameCode

### API Gateway Logs

**Enable detailed logging (if needed):**
1. API Gateway Console
2. Select your WebSocket API
3. Stages → dev → Logs/Tracing
4. Enable "Enable CloudWatch Logs"

## Common Issues & Solutions

### Issue: "Unable to import module 'Function'"

**Cause:** Lambda handler path mismatch

**Solution:** Verify handler in `template.yaml` matches assembly name:
```yaml
Handler: ChessOfCards.ConnectionHandler::ChessOfCards.ConnectionHandler.Function::FunctionHandler
```

### Issue: Connection closes immediately

**Cause:** Lambda timeout or unhandled exception

**Solution:** Check CloudWatch Logs for errors

### Issue: "Endpoint is not connected" when sending messages

**Cause:** Connection was closed or doesn't exist

**Solution:**
- Reconnect before sending messages
- Check connection still exists in DynamoDB

### Issue: "ConditionalCheckFailedException"

**Cause:** Optimistic locking failure (version mismatch)

**Solution:** This is expected behavior - retry the operation. The Lambda should handle this automatically in Phase 3+.

### Issue: Table doesn't exist

**Cause:** Deployment didn't complete or wrong environment

**Solution:**
- Check CloudFormation stack status
- Verify environment parameter matches

## Browser-Based Testing

Create a simple HTML file for testing:

```html
<!DOCTYPE html>
<html>
<head>
    <title>Chess of Cards Test Client</title>
</head>
<body>
    <h1>Chess of Cards Test Client</h1>

    <div>
        <label>WebSocket URL:</label>
        <input id="wsUrl" type="text" size="60"
               value="wss://YOUR-ENDPOINT.execute-api.us-east-2.amazonaws.com/dev">
        <button onclick="connect()">Connect</button>
        <button onclick="disconnect()">Disconnect</button>
    </div>

    <div>
        <h3>Create Game</h3>
        <input id="hostName" placeholder="Host Name">
        <select id="duration">
            <option>SHORT</option>
            <option selected>MEDIUM</option>
            <option>LONG</option>
        </select>
        <button onclick="createGame()">Create Game</button>
    </div>

    <div>
        <h3>Join Game</h3>
        <input id="gameCode" placeholder="Game Code">
        <input id="guestName" placeholder="Guest Name">
        <button onclick="joinGame()">Join Game</button>
    </div>

    <div>
        <h3>Messages</h3>
        <textarea id="messages" rows="20" cols="80" readonly></textarea>
    </div>

    <script>
        let ws;

        function connect() {
            const url = document.getElementById('wsUrl').value;
            ws = new WebSocket(url);

            ws.onopen = () => {
                log('Connected!');
            };

            ws.onmessage = (event) => {
                const msg = JSON.parse(event.data);
                log('Received: ' + JSON.stringify(msg, null, 2));
            };

            ws.onerror = (error) => {
                log('Error: ' + error);
            };

            ws.onclose = () => {
                log('Disconnected');
            };
        }

        function disconnect() {
            if (ws) ws.close();
        }

        function createGame() {
            const hostName = document.getElementById('hostName').value;
            const duration = document.getElementById('duration').value;

            const message = {
                action: 'createPendingGame',
                data: {
                    hostName: hostName,
                    durationOption: duration
                }
            };

            send(message);
        }

        function joinGame() {
            const gameCode = document.getElementById('gameCode').value;
            const guestName = document.getElementById('guestName').value;

            const message = {
                action: 'joinGame',
                data: {
                    gameCode: gameCode,
                    guestName: guestName
                }
            };

            send(message);
        }

        function send(message) {
            if (!ws || ws.readyState !== WebSocket.OPEN) {
                log('Not connected!');
                return;
            }

            const json = JSON.stringify(message);
            log('Sending: ' + json);
            ws.send(json);
        }

        function log(message) {
            const textarea = document.getElementById('messages');
            textarea.value += message + '\n\n';
            textarea.scrollTop = textarea.scrollHeight;
        }
    </script>
</body>
</html>
```

Save as `test-client.html` and open in browser.

## Success Criteria

### Phase 1 & 2 Testing Complete When:

- [x] Can connect to WebSocket successfully
- [x] Connection record appears in DynamoDB
- [x] Disconnection cleans up properly
- [x] Can create a pending game
- [x] Game code is generated
- [x] Can join game with valid code
- [x] Both players receive GameStarted message
- [x] Pending game is removed after joining
- [x] Active game is created
- [x] Invalid game codes are handled
- [x] Can delete pending game
- [x] Disconnect during game creates timer
- [x] Opponent is notified of disconnection

## Next Steps After Testing

Once all tests pass:
1. Document any issues found
2. Fix critical bugs
3. Optimize if needed
4. Proceed to Phase 3 (Core Gameplay)

If issues are found:
1. Check CloudWatch Logs for errors
2. Verify DynamoDB table contents
3. Check SAM template configuration
4. Review Lambda function code
5. Test locally with SAM Local if needed

## Clean Up (Optional)

To remove all deployed resources:

```bash
sam delete --stack-name chess-of-cards-api-dev
```

This will delete everything and stop incurring AWS costs.
