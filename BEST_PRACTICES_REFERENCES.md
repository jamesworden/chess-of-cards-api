# AWS Best Practices References

This document contains all the AWS documentation and references that informed the best practices recommendations for this project.

---

## Official AWS Documentation

### 1. Serverless Game Backend Architecture
**Source:** AWS Well-Architected Framework - Games Industry Lens
**URL:** https://docs.aws.amazon.com/wellarchitected/latest/games-industry-lens/serverless-backend.html

**Key Takeaways:**
- **Lambda for game logic:** "Lambda gives you the flexibility to write each game feature as a separate microservice"
- **WebSockets for point-to-point:** "WebSockets implementation is suitable for point-to-point communication, where players establish WebSockets connections to Amazon API Gateway WebSockets"
- **Separate data stores:** "Use separate data stores for each game feature's data storage needs" (DynamoDB per feature)
- **AWS IoT Core for broadcasting:** "WebSockets over MQTT is used to support use cases such as broadcasting live in-game updates to all connected players"

**Why this validates our architecture:**
- We use Lambda for all game features ✓
- We use API Gateway WebSocket for 1v1 (point-to-point) ✓
- We have separate DynamoDB tables per feature (connections, pending games, active games, timers) ✓

---

### 2. Building a Serverless Multi-Player Game That Scales
**Source:** AWS Compute Blog
**URL:** https://aws.amazon.com/blogs/compute/building-a-serverless-multiplayer-game-that-scales/

**Key Takeaways:**
- Reference architecture for serverless trivia game using WebSocket API + Lambda
- Demonstrates connection management patterns
- Shows proper DynamoDB table design for game state
- Emphasizes cost efficiency of serverless approach

**GitHub Sample Code:**
https://github.com/aws-samples/serverless-trivia-game

**Why this validates our architecture:**
- AWS's own reference implementation uses the same pattern we're using
- Shows connection lifecycle management (connect/disconnect handlers)
- Demonstrates proper message routing

---

### 3. Lambda Best Practices
**Source:** AWS Lambda Developer Guide
**URL:** https://docs.aws.amazon.com/lambda/latest/dg/best-practices.html

**Specific Recommendations We're Implementing:**

#### Static Initialization (High Priority #1)
**Quote:** "Take advantage of execution environment reuse to improve the performance of your function. Initialize SDK clients and database connections outside of the function handler, and cache static assets locally in the /tmp directory."

**Why:** Lambda containers are reused across invocations. Initializing outside the handler reduces overhead.

#### Connection Reuse (High Priority #2)
**Quote:** "Keep alive the connections that your function uses. Use a keep-alive directive to maintain persistent connections. Lambda purges idle connections over time."

**Why:** DynamoDB/API Gateway clients should maintain connection pools.

#### Provisioned Concurrency (Medium Priority #4)
**URL:** https://docs.aws.amazon.com/lambda/latest/dg/provisioned-concurrency.html
**Quote:** "Provisioned concurrency initializes a requested number of execution environments so that they are prepared to respond immediately to your function's invocations."

**Why:** Eliminates cold starts for latency-sensitive game actions.

---

### 4. API Gateway WebSocket API Documentation
**Source:** Amazon API Gateway Developer Guide
**URL:** https://docs.aws.amazon.com/apigateway/latest/developerguide/apigateway-websocket-api.html

**Key Sections:**

#### Managing Connected Users ($connect and $disconnect)
**URL:** https://docs.aws.amazon.com/apigateway/latest/developerguide/apigateway-websocket-api-route-keys-connect-disconnect.html

**Quote on $disconnect reliability:** "The $disconnect route is executed after the connection is closed, so there is no way to send data to the client using the connection. API Gateway makes a best-effort attempt to deliver the $disconnect event to your integration, but it cannot guarantee delivery."

**Why this matters (Medium Priority #5):**
- We need reconnection grace period because $disconnect is unreliable
- Client may disconnect briefly without triggering handler
- Must handle rapid reconnection scenarios

#### WebSocket API Throttling
**URL:** https://docs.aws.amazon.com/apigateway/latest/developerguide/websocket-api-protect.html

**Why (Medium Priority #6):**
- Protects against abuse and ensures fair resource usage
- Prevents DoS attacks

#### WebSocket Lambda Authorizers
**URL:** https://docs.aws.amazon.com/apigateway/latest/developerguide/apigateway-websocket-api-lambda-auth.html

**Why (Low Priority #7):**
- Secure WebSocket connections
- Validate JWT tokens before connection establishment

---

### 5. DynamoDB Best Practices
**Source:** Amazon DynamoDB Developer Guide
**URL:** https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/best-practices.html

**Key Recommendations:**

#### Connection Pooling
**Why (High Priority #2):**
- DynamoDB clients should be configured with proper connection limits
- Reduces latency by reusing connections

#### GSI Design Patterns
**Quote:** "Use global secondary indexes to support additional access patterns"

**Why our design is correct:**
- ConnectionsTable: GameCodeIndex (query by game)
- PendingGamesTable: HostConnectionIndex (query by host)
- ActiveGamesTable: HostConnectionIndex + GuestConnectionIndex (query by either player)
- GameTimersTable: ExpiryIndex (query expired timers)

#### Time-to-Live (TTL)
**URL:** https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/TTL.html

**Why we're using it correctly:**
- Automatic cleanup of expired connections
- No Lambda required for cleanup (saves money)
- Prevents DynamoDB from growing unbounded

---

### 6. AWS X-Ray with Lambda
**Source:** AWS Lambda Developer Guide
**URL:** https://docs.aws.amazon.com/lambda/latest/dg/services-xray.html

**Key Takeaways:**
- Distributed tracing across Lambda, DynamoDB, API Gateway
- Visualize request flow through entire WebSocket lifecycle
- Identify performance bottlenecks

**Why (High Priority #3):**
- Essential for debugging WebSocket flows (connect → action → disconnect)
- Helps identify slow DynamoDB queries
- Shows API Gateway → Lambda latency

---

### 7. CloudWatch Custom Metrics
**Source:** Amazon CloudWatch User Guide
**URL:** https://docs.aws.amazon.com/AmazonCloudWatch/latest/monitoring/publishingMetrics.html

**Why (Low Priority #9):**
- Track game-specific metrics (active games, average duration, etc.)
- Set up alarms for business metrics
- Better than just Lambda execution metrics

---

## Comparison: API Gateway WebSocket vs AppSync

### API Gateway WebSocket
**Best for:** Point-to-point communication (1v1, small groups)
**Use case:** Our 2-player Chess game ✓

**Pros:**
- Simpler for small-scale messaging
- Lower cost for point-to-point
- Better local testing (sam local)
- More control over connection lifecycle

**Cons:**
- Manual connection state management (we handle this with DynamoDB)
- Must call API Gateway Management API one connection at a time

### AppSync GraphQL Subscriptions
**Best for:** Complex data fetching, real-time data sync, offline support
**Use case:** Apps with complex nested queries, many-to-many relationships

**Pros:**
- Automatic subscription management
- Better for mass broadcast scenarios
- Built-in caching and offline support
- GraphQL query language

**Cons:**
- Overkill for simple 2-player game
- More expensive (charged per query/mutation/subscription)
- Poor local testing support
- Less control over WebSocket behavior

### AppSync Events (New in 2024)
**Best for:** Mass broadcast (millions of subscribers)
**Use case:** Live leaderboards, tournament brackets, spectator mode

**Pricing:** $0.08 per million connection minutes (vs $0.25 for API Gateway)

**Pros:**
- 3x cheaper than API Gateway for broadcast scenarios
- Handles millions of concurrent subscribers
- Automatic fan-out

**Cons:**
- Overkill for 2-player game
- No local testing support
- Less mature (newer service)

### AWS IoT Core (MQTT)
**Best for:** Massive scale IoT devices, pub/sub messaging
**Use case:** MMO games with thousands of players per match

**Cons:**
- Most expensive option
- Complex setup
- Overkill for 2-player game

---

## Architectural Decision Summary

### Why API Gateway WebSocket + Lambda is Best for This Project

**Criteria Analysis:**

| Requirement | API GW WebSocket | AppSync | AppSync Events | IoT Core |
|-------------|------------------|---------|----------------|----------|
| **1v1 point-to-point** | ✅ Perfect | ⚠️ Overkill | ⚠️ Overkill | ❌ Overkill |
| **Cost efficiency** | ✅ Cheapest | ⚠️ 3x more | ✅ Cheap | ❌ Expensive |
| **Local testing** | ✅ Excellent | ❌ Poor | ❌ None | ❌ Complex |
| **Serverless** | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes |
| **AWS recommended** | ✅ Yes* | ⚠️ For different use case | ⚠️ For broadcast | ⚠️ For IoT |

*For point-to-point multiplayer games per AWS Well-Architected Framework

---

## Community & Industry Validation

### Serverless Guru: AWS AppSync vs API Gateway
**URL:** https://www.serverlessguru.com/tips/aws-appsync-vs-amazon-api-gateway

**Quote:** "API Gateway WebSocket APIs work fine for simple use cases where you need to send messages to a small group of users at a time, like 1-2-1 private chat or even group chats."

**Validation:** Our 2-player game is exactly this use case.

### AWS Heroes: AppSync Events Analysis
**URL:** https://www.ranthebuilder.cloud/post/appsync-events-websockets

**Quote:** "If you don't require mass broadcast and always send user-specific messages, as regular API GW websockets are a solid, battle-tested option."

**Validation:** We send user-specific messages (player A → player B), not broadcast.

### Ready, Set, Cloud: AppSync Events Review
**URL:** https://www.readysetcloud.io/blog/allen.helton/appsync-events/

**Quote on local testing:** "Local development story is non-existent for AppSync Events"

**Validation:** Our requirement for easy local testing makes API Gateway WebSocket the clear choice.

---

## Additional AWS Resources

### SAM CLI Documentation
**URL:** https://docs.aws.amazon.com/serverless-application-model/latest/developerguide/what-is-sam.html

- `sam local start-api` for WebSocket testing
- `sam build --use-container` for Lambda-compatible builds
- `sam deploy --guided` for interactive deployment

### WebSocket API Tutorial (Chat App)
**URL:** https://docs.aws.amazon.com/apigateway/latest/developerguide/websocket-api-chat-app.html

**Why relevant:**
- Official AWS tutorial using exact same pattern we're using
- Shows connection management with DynamoDB
- Demonstrates message broadcasting to multiple connections
- Our implementation mirrors this pattern

---

## Performance Benchmarks (AWS Re:Invent Presentations)

### Lambda Cold Start Times (.NET 8)
- **Without Provisioned Concurrency:** 200-800ms
- **With Provisioned Concurrency:** <10ms

**Source:** AWS re:Invent sessions on Lambda performance optimization

**Why we recommend Provisioned Concurrency (Medium Priority #4):**
- Game actions need <100ms response time for good UX
- Cold starts can exceed this threshold
- Cost is only ~$12/month for 1 warm instance

### WebSocket Connection Limits
- **API Gateway WebSocket:** 128,000 concurrent connections per account
- **Message rate:** 10,000 messages/second per WebSocket API
- **Connection duration:** Up to 2 hours

**Source:** API Gateway WebSocket quotas
**URL:** https://docs.aws.amazon.com/apigateway/latest/developerguide/limits.html

**Why this is sufficient:**
- Our game has 2 players per game
- Even with 10,000 concurrent games = 20,000 connections (well under limit)
- Message rate is turn-based, not real-time shooter frequency

---

## Cost Analysis

### Estimated Monthly Costs (1,000 active games/month)

**Current Architecture (API Gateway WebSocket + Lambda):**
- API Gateway: $0.25/million connection minutes
  - Average game: 30 minutes × 2 players = 60 connection-minutes
  - 1,000 games = 60,000 connection-minutes = **$0.02**
- Lambda invocations: $0.20/million requests
  - Average game: 50 moves × 2 messages = 100 invocations
  - 1,000 games = 100,000 invocations = **$0.02**
- Lambda compute: $0.0000166667/GB-second
  - 100,000 invocations × 1s × 1GB = **$1.67**
- DynamoDB: Pay-per-request
  - ~500,000 read/write units = **$0.63**

**Total: ~$2.34/month for 1,000 games**

**With Provisioned Concurrency (+1 warm Lambda):**
- Add: $13.50/month (1 instance × 730 hours × $0.015/hour + compute)

**Total with optimizations: ~$15.84/month for 1,000 games**

**vs Dedicated Server (EC2 t3.small):**
- Instance: $15.18/month (730 hours × $0.0208/hour)
- Load Balancer: $16.20/month
- **Total: $31.38/month** (and doesn't auto-scale)

**Conclusion: Serverless is ~50% cheaper even with optimizations**

---

## When to Reconsider Architecture

### Signals that you might need a different approach:

1. **Extremely high message frequency**
   - If game becomes action-based (not turn-based) with >10 messages/second
   - Consider: Dedicated WebSocket server (EC2 + ALB) or Fargate

2. **Ultra-low latency requirements (<50ms)**
   - If competitive gameplay requires <50ms message delivery
   - Consider: Dedicated infrastructure in multiple regions

3. **Need for custom WebSocket protocols**
   - If you need custom WebSocket subprotocols or extensions
   - API Gateway WebSocket has limitations on protocol customization

4. **Mass broadcast scenarios**
   - If you add spectator mode with 1000s of viewers per game
   - Consider: AppSync Events or AWS IoT Core

**Current status: None of these apply to your turn-based 2-player game**

---

## Related AWS Blog Posts

### "Building Serverless Applications That Scale"
**URL:** https://aws.amazon.com/blogs/compute/building-a-serverless-multi-player-game-that-scales-part-3/

Key insights on DynamoDB access patterns for game state.

### "Serverless ICYMI Q3 2025"
**URL:** https://aws.amazon.com/blogs/compute/serverless-icymi-q3-2025/

Latest updates to Lambda, API Gateway, and serverless services.

---

## Summary

**The research conclusively shows that API Gateway WebSocket + Lambda + DynamoDB is the AWS-recommended pattern for:**

1. ✅ Point-to-point multiplayer games (1v1, small groups)
2. ✅ Turn-based gameplay (not ultra-high frequency)
3. ✅ Serverless cost efficiency
4. ✅ Easy local testing requirements

**Alternative patterns (AppSync, IoT Core) are recommended for:**
- ❌ Mass broadcast scenarios (1000s of subscribers)
- ❌ Complex data fetching with nested relationships
- ❌ IoT device management at scale

**Your architecture matches AWS official guidance and reference implementations.**

---

Last updated: 2025-10-13
