# Best Practices Implementation TODO

This document tracks the implementation of AWS serverless best practices for the Chess of Cards WebSocket API.

## Overview

Our current architecture (API Gateway WebSocket + Lambda + DynamoDB) matches AWS's recommended pattern for serverless multiplayer games. This TODO list addresses optimization gaps to make the implementation production-ready.

**Current Score: 80/100**
**Target Score: 95/100**

---

## High Priority (Do Now)

### 1. Fix Static Initialization in Lambda Functions
**Status:** ✅ Completed (2025-10-13)
**Impact:** Reduces per-invocation overhead, improves response time
**Effort:** 15 minutes

**Problem:** ServiceProvider is recreated on every Lambda invocation, causing unnecessary overhead.

**Files to modify:**
- `src/ChessOfCards.GameActionHandler/Function.cs`
- `src/ChessOfCards.ConnectionHandler/Function.cs`

**Changes:**
```csharp
// Change from:
private readonly ActionDispatcher _actionDispatcher;

public Function()
{
    var serviceProvider = ServiceConfiguration.ConfigureServices();
    // ...
}

// To:
private static readonly IServiceProvider ServiceProvider = ServiceConfiguration.ConfigureServices();
private static readonly ActionDispatcher ActionDispatcher = InitializeDispatcher();

private static ActionDispatcher InitializeDispatcher()
{
    var mediator = ServiceProvider.GetRequiredService<IMediator>();
    var webSocketService = ServiceProvider.GetRequiredService<WebSocketService>();
    return new ActionDispatcher(mediator, webSocketService);
}

public Function()
{
    // Empty constructor - use static fields
}
```

**AWS Reference:** [Lambda best practices - Take advantage of execution environment reuse](https://docs.aws.amazon.com/lambda/latest/dg/best-practices.html)

---

### 2. Add DynamoDB Connection Pooling
**Status:** ✅ Completed (2025-10-13)
**Impact:** Reduces latency, better connection reuse
**Effort:** 10 minutes

**Problem:** DynamoDB client not configured for optimal connection pooling.

**Files to modify:**
- `src/ChessOfCards.GameActionHandler/Configuration/ServiceConfiguration.cs`
- `src/ChessOfCards.ConnectionHandler/Configuration/ServiceConfiguration.cs` (if exists)

**Changes:**
```csharp
// Change from:
services.AddSingleton<IAmazonDynamoDB>(_ => new AmazonDynamoDBClient());

// To:
services.AddSingleton<IAmazonDynamoDB>(_ =>
{
    var config = new AmazonDynamoDBConfig
    {
        MaxConnectionsPerServer = 50,  // Enable connection pooling
        Timeout = TimeSpan.FromSeconds(10),
        MaxErrorRetry = 3  // Built-in retry logic
    };
    return new AmazonDynamoDBClient(config);
});
```

**AWS Reference:** [DynamoDB best practices](https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/best-practices.html)

---

### 3. Enable X-Ray Tracing
**Status:** ✅ Completed (2025-10-13)
**Impact:** Better debugging, distributed tracing visibility
**Effort:** 5 minutes

**Problem:** No distributed tracing for debugging WebSocket flows.

**Files to modify:**
- `template.yaml`

**Changes:**
```yaml
Globals:
  Function:
    Timeout: 30
    Runtime: dotnet8
    MemorySize: 512
    LoggingConfig:
      LogFormat: JSON
    Tracing: Active  # ADD THIS LINE
    Environment:
      Variables:
        # ... existing vars
```

**Additional:** Add X-Ray SDK NuGet package:
```bash
dotnet add src/ChessOfCards.Infrastructure package AWSXRayRecorder.Core
dotnet add src/ChessOfCards.Infrastructure package AWSXRayRecorder.Handlers.AwsSdk
```

**Initialize in code:**
```csharp
// Add to ServiceConfiguration.cs
using Amazon.XRay.Recorder.Handlers.AwsSdk;

public static IServiceProvider ConfigureServices()
{
    AWSSDKHandler.RegisterXRayForAllServices(); // Add this line
    // ... rest of configuration
}
```

**AWS Reference:** [Using AWS X-Ray with Lambda](https://docs.aws.amazon.com/lambda/latest/dg/services-xray.html)

---

## Medium Priority (Before Production)

### 4. Add Provisioned Concurrency for GameActionHandler
**Status:** ⏳ Not Started
**Impact:** Eliminates cold starts on critical path
**Effort:** 10 minutes
**Cost:** ~$12/month for 1 warm instance

**Problem:** Cold starts can add 100-500ms latency for game actions.

**Files to modify:**
- `template.yaml`

**Changes:**
```yaml
GameActionHandlerFunction:
  Type: AWS::Serverless::Function
  Properties:
    # ... existing properties ...
    AutoPublishAlias: live
    ProvisionedConcurrencyConfig:
      ProvisionedConcurrentExecutions: 1  # Keep 1 instance warm
```

**Notes:**
- Only add to GameActionHandler (not ConnectionHandler) since game actions are more latency-sensitive
- Monitor CloudWatch metrics after deployment to adjust count if needed
- Can be disabled in dev environment to save costs

**AWS Reference:** [Provisioned Concurrency](https://docs.aws.amazon.com/lambda/latest/dg/provisioned-concurrency.html)

---

### 5. Implement Reconnection Grace Period
**Status:** ⏳ Not Started
**Impact:** Better UX, prevents game loss on brief disconnects
**Effort:** 2-3 hours

**Problem:** `$disconnect` is "best effort" - client may disconnect/reconnect rapidly, losing game state.

**Files to modify:**
- `src/ChessOfCards.Infrastructure/Models/ConnectionRecord.cs`
- `src/ChessOfCards.ConnectionHandler/Function.cs`
- Add new `ReconnectHandler.cs`

**Design:**
1. Add fields to `ConnectionRecord`:
```csharp
public string? ReconnectionToken { get; set; }  // JWT or GUID
public DateTime? DisconnectedAt { get; set; }
public DateTime? LastHeartbeat { get; set; }
```

2. On `$disconnect`:
   - Mark connection as `DisconnectedAt = DateTime.UtcNow`
   - Keep record in DynamoDB (don't delete immediately)
   - Set TTL for 60 seconds from now
   - Notify opponent: "Player temporarily disconnected"

3. On `$connect` with reconnection token:
   - Look up old connection by token
   - If within grace period (30s), restore game state
   - Update connectionId, clear DisconnectedAt
   - Notify opponent: "Player reconnected"

4. Add new action: `reconnect`
```json
{
  "action": "reconnect",
  "reconnectionToken": "abc123",
  "gameCode": "XYZ789"
}
```

**AWS Reference:** [Managing WebSocket connections](https://docs.aws.amazon.com/apigateway/latest/developerguide/apigateway-websocket-api-route-keys-connect-disconnect.html) (Note: mentions $disconnect is best-effort)

---

### 6. Add WebSocket Route Throttling
**Status:** ⏳ Not Started
**Impact:** Protects against abuse, ensures fair usage
**Effort:** 5 minutes

**Problem:** No rate limiting on WebSocket routes.

**Files to modify:**
- `template.yaml`

**Changes:**
```yaml
WebSocketStage:
  Type: AWS::ApiGatewayV2::Stage
  Properties:
    ApiId: !Ref ChessWebSocketApi
    StageName: !Ref Environment
    AutoDeploy: true
    DefaultRouteSettings:
      LoggingLevel: INFO
      DataTraceEnabled: true
      DetailedMetricsEnabled: true
      ThrottlingBurstLimit: 100   # ADD THIS
      ThrottlingRateLimit: 50     # ADD THIS (requests per second)
```

**Recommended limits:**
- **Dev:** BurstLimit: 100, RateLimit: 50
- **Prod:** BurstLimit: 500, RateLimit: 200

**AWS Reference:** [Throttling WebSocket APIs](https://docs.aws.amazon.com/apigateway/latest/developerguide/websocket-api-protect.html)

---

## Low Priority (Post-Launch)

### 7. Add Lambda Authorizer for Authentication
**Status:** ⏳ Not Started
**Impact:** Secure connections, prevent unauthorized access
**Effort:** 4-6 hours
**Depends on:** User authentication system (Cognito, Auth0, etc.)

**Problem:** Currently `AuthorizationType: NONE` - anyone can connect.

**High-level design:**
1. Create new Lambda function: `WebSocketAuthorizerFunction`
2. Implement JWT validation (from Cognito or custom provider)
3. Return IAM policy allowing/denying connection
4. Update `$connect` route to use authorizer

**Files to create:**
- `src/ChessOfCards.Authorizer/Function.cs`

**Files to modify:**
- `template.yaml`

**Template changes:**
```yaml
WebSocketAuthorizer:
  Type: AWS::ApiGatewayV2::Authorizer
  Properties:
    Name: WebSocketAuthorizer
    ApiId: !Ref ChessWebSocketApi
    AuthorizerType: REQUEST
    AuthorizerUri: !Sub "arn:aws:apigateway:${AWS::Region}:lambda:path/2015-03-31/functions/${WebSocketAuthorizerFunction.Arn}/invocations"
    IdentitySource:
      - route.request.querystring.token

ConnectRoute:
  Type: AWS::ApiGatewayV2::Route
  Properties:
    ApiId: !Ref ChessWebSocketApi
    RouteKey: $connect
    AuthorizationType: CUSTOM  # Change from NONE
    AuthorizerId: !Ref WebSocketAuthorizer
    Target: !Sub "integrations/${ConnectIntegration}"
```

**Client connection:**
```javascript
// Client must pass token in query string
const ws = new WebSocket('wss://api.example.com/dev?token=eyJhbGc...');
```

**AWS Reference:** [WebSocket API Lambda authorizers](https://docs.aws.amazon.com/apigateway/latest/developerguide/apigateway-websocket-api-lambda-auth.html)

---

### 8. Implement Game Clock Timer System
**Status:** ⏳ Not Started
**Impact:** Core game feature for timed gameplay
**Effort:** 8-10 hours
**Phase:** Phase 5 (per PROJECT_STATUS.md)

**Problem:** No timer enforcement for player moves.

**Design:**
- Use `GameTimersTable` (already defined in template.yaml)
- EventBridge scheduled rule (every 1 minute) triggers `TimerHandlerFunction`
- Query `ExpiryIndex` GSI for expired timers
- Process expired timers (forfeit on timeout, disconnect grace period, etc.)

**Files to create:**
- `src/ChessOfCards.TimerHandler/Function.cs`

**Files to modify:**
- Uncomment `TimerHandlerFunction` in `template.yaml` (lines 111-140)

**Reference implementation:**
- See commented code in `template.yaml` lines 111-140

---

### 9. Add CloudWatch Custom Metrics
**Status:** ⏳ Not Started
**Impact:** Better monitoring, alerting, capacity planning
**Effort:** 2-3 hours

**Problem:** Only have default Lambda metrics, no game-specific metrics.

**Custom metrics to add:**
- `ActiveGames` - Current number of active games
- `PendingGames` - Current number of games awaiting players
- `AverageGameDuration` - Average game length
- `ConnectionErrors` - Failed WebSocket connections
- `MoveLatency` - Time from move submission to opponent notification

**Implementation:**
```csharp
using Amazon.CloudWatch;
using Amazon.CloudWatch.Model;

// Add to ServiceConfiguration
services.AddSingleton<IAmazonCloudWatch>(_ => new AmazonCloudWatchClient());

// Usage in handlers
await cloudWatch.PutMetricDataAsync(new PutMetricDataRequest
{
    Namespace = "ChessOfCards/GameMetrics",
    MetricData = new List<MetricDatum>
    {
        new MetricDatum
        {
            MetricName = "ActiveGames",
            Value = activeGameCount,
            Unit = StandardUnit.Count,
            Timestamp = DateTime.UtcNow
        }
    }
});
```

**AWS Reference:** [CloudWatch Custom Metrics](https://docs.aws.amazon.com/AmazonCloudWatch/latest/monitoring/publishingMetrics.html)

---

### 10. Implement Connection Health Monitoring
**Status:** ⏳ Not Started
**Impact:** Detect stale connections, improve reliability
**Effort:** 3-4 hours

**Problem:** No heartbeat/ping mechanism to detect truly dead connections.

**Design:**
1. Add `lastHeartbeat` field to `ConnectionRecord`
2. Client sends periodic `ping` action (every 30s)
3. Server responds with `pong`
4. Background job (EventBridge + Lambda) checks for stale connections (no heartbeat in 2 minutes)
5. Clean up stale connections, notify opponents

**Client implementation:**
```javascript
setInterval(() => {
  ws.send(JSON.stringify({ action: 'ping' }));
}, 30000);
```

**Server implementation:**
```csharp
// In GameActionHandler
case "ping":
    await connectionRepository.UpdateLastHeartbeatAsync(connectionId);
    await webSocketService.SendMessageAsync(connectionId, new { MessageType = "pong" });
    break;
```

---

## Additional Considerations

### Performance Optimization
- [ ] Consider moving to ARM64 architecture for 20% cost savings
- [ ] Implement DynamoDB DAX (cache) if latency becomes critical
- [ ] Use Lambda layers for shared dependencies

### Security Hardening
- [ ] Implement request validation schemas
- [ ] Add AWS WAF rules for WebSocket API
- [ ] Enable AWS Shield Standard (free)
- [ ] Implement game action rate limiting per connection

### Monitoring & Alerting
- [ ] Set up CloudWatch alarms for Lambda errors
- [ ] Set up CloudWatch alarms for DynamoDB throttling
- [ ] Create CloudWatch dashboard for real-time metrics
- [ ] Set up SNS notifications for critical errors

### Cost Optimization
- [ ] Enable DynamoDB on-demand backup
- [ ] Set up AWS Budgets alerts
- [ ] Review CloudWatch log retention periods
- [ ] Consider S3 for game replay storage (vs DynamoDB)

---

## Reference Documentation

See `BEST_PRACTICES_REFERENCES.md` for complete list of AWS documentation that informed these recommendations.

---

## Progress Tracking

**Completed:** 3/10 ✅
**In Progress:** 0/10
**Not Started:** 7/10

### Completed Items:
1. ✅ Fix Static Initialization in Lambda Functions (2025-10-13)
2. ✅ Add DynamoDB Connection Pooling (2025-10-13)
3. ✅ Enable X-Ray Tracing (2025-10-13)

### Next Steps:
- Deploy changes and monitor CloudWatch metrics
- Consider adding Provisioned Concurrency (Medium Priority #4) if cold starts are observed
- Implement reconnection grace period for better UX (Medium Priority #5)

Last updated: 2025-10-13
