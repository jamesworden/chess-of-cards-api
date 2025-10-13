# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a .NET 8 serverless application deployed to AWS Lambda using SAM (Serverless Application Model). The application is an ASP.NET Core Web API that provides a REST API backed by DynamoDB. Despite the name "chess-of-cards-api" and controller name "Books", the entities are generic and use `GameId` as the primary identifier.

## Technology Stack

- **Runtime**: .NET 8 / C#
- **Framework**: ASP.NET Core Web API with minimal API support
- **AWS Services**: Lambda, API Gateway (HTTP API), DynamoDB
- **Infrastructure**: AWS SAM for deployment
- **Testing**: xUnit with Bogus for test data generation

## Essential Commands

### Building and Testing

```bash
# Build the SAM application
sam build

# Build with Docker container (required for Lambda compatibility)
sam build --use-container --mount-with WRITE

# Run unit tests
dotnet test tests/ServerlessAPI.Tests/ServerlessAPI.Tests.csproj
```

### Local Development

```bash
# Start API locally on port 3000
sam local start-api

# Invoke a specific Lambda function with test event
sam local invoke NetCodeWebAPIServerless --event events/event.json

# Test the local API
curl http://localhost:3000/
curl http://localhost:3000/api/books
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

# View logs from deployed Lambda
sam logs -n NetCodeWebAPIServerless --stack-name chess-of-cards-api --tail
```

### Validation

```bash
# Validate SAM template with linting
sam validate --lint
```

## Architecture

### Application Structure

```
src/ServerlessAPI/
├── Program.cs                 # Application entry point, DI configuration
├── Controllers/
│   └── BooksController.cs     # REST API endpoints (CRUD operations)
├── Entities/
│   └── Book.cs               # DynamoDB entity model with mapping attributes
└── Repositories/
    ├── IBookRepository.cs    # Repository interface
    └── BookRepository.cs     # DynamoDB data access implementation
```

### Dependency Injection Setup (Program.cs)

The application uses standard ASP.NET Core DI with AWS-specific registrations:
- `IAmazonDynamoDB` - DynamoDB client (singleton)
- `IDynamoDBContext` - DynamoDB context for ORM operations (scoped)
- `IBookRepository` - Business logic repository (scoped)
- `AddAWSLambdaHosting(LambdaEventSource.HttpApi)` - Replaces Kestrel with Lambda hosting

### DynamoDB Entity Mapping

The `Book` entity uses DynamoDB attributes for ORM mapping:
- `[DynamoDBTable("chess-of-cards-api-game")]` - Maps to table (hardcoded, overridden by environment variable)
- `[DynamoDBHashKey]` - Partition key (GameId)
- `[DynamoDBProperty]` - Standard properties
- `[DynamoDBIgnore]` - Excluded from persistence

The actual table name is determined by the `SAMPLE_TABLE` environment variable, which is set dynamically per environment via `template.yaml`.

### Repository Pattern

`BookRepository` implements standard CRUD operations:
- Uses `IDynamoDBContext` for high-level DynamoDB operations
- Uses `ScanOperationConfig` for listing with limits
- All operations include error handling and structured logging

## Infrastructure (template.yaml)

### Key Resources

- **NetCodeWebAPIServerless** - Lambda function running ASP.NET Core
  - Runtime: dotnet8
  - Handler: ServerlessAPI (assembly name)
  - Memory: 1024 MB
  - Timeout: 100 seconds (global)

- **GameTable** - DynamoDB table
  - Table name: `chess-of-cards-api-game-{Environment}`
  - Primary key: GameId (String)
  - Provisioned capacity: 2 RCU / 2 WCU

### Environment Parameters

The stack supports `Environment` parameter (dev/prod) which:
- Determines the DynamoDB table name suffix
- Is passed to Lambda via `ENVIRONMENT_NAME` environment variable
- Defaults to "dev"

### API Gateway Configuration

- Uses HTTP API (not REST API) with PayloadFormatVersion 2.0
- Two event sources: `/{proxy+}` and `/` for catch-all routing
- ASP.NET Core routing handles all path mapping

## CI/CD Pipeline (.github/workflows/main.yml)

### Workflow Stages

1. **Build Job** - Runs on all pushes to main/develop
   - Builds SAM application with Docker
   - Uploads artifacts for deployment jobs

2. **Deploy Dev** - Runs only on pushes to `develop` branch
   - Deploys to dev environment with `Environment=dev`
   - Uses GitHub environment: `dev`

3. **Deploy Prod** - Runs only on pushes to `main` branch
   - Deploys to production with `Environment=prod`
   - Uses GitHub environment: `prod`

### Important Notes

- SAM build uses `--mount-with WRITE` flag to avoid interactive prompts in CI
- Deployments use separate S3 bucket: `chess-of-cards-api-artifacts`
- Stack names: `ChessOfCardsApi-Dev` and `ChessOfCardsApi-Prod`
- AWS credentials are stored as environment-specific secrets

## Testing

### Test Structure

```
tests/ServerlessAPI.Tests/
├── BookControllerTest.cs       # Controller unit tests
└── MockBookRepository.cs       # Mock repository implementation
```

### Testing Approach

- Uses xUnit test framework
- Uses `Microsoft.AspNetCore.Mvc.Testing` for controller testing
- Uses Bogus library for generating test data
- Uses mocks (MockBookRepository) instead of DynamoDB for unit tests
- Project has `InternalsVisibleTo` attribute for test access

## Configuration Files

- **samconfig.toml** - SAM CLI configuration with build/deploy defaults
- **omnisharp.json** - C# IDE configuration
- **aws-lambda-tools-defaults.json** - AWS Lambda tooling defaults (in src/ServerlessAPI/)

## Important Implementation Notes

### Naming Inconsistencies

The codebase has legacy naming issues to be aware of:
- The entity is called `Book` but uses `GameId` as primary key
- Controller property references: `new { id = book.GameId }` in POST
- Comments refer to "books" but the domain appears to be games

### DynamoDB Table Configuration

- Table name in `Book.cs` is hardcoded but overridden by environment variable `SAMPLE_TABLE`
- Template creates environment-specific tables: `chess-of-cards-api-game-{Environment}`
- This allows dev/prod isolation

### Logging

- Uses structured JSON logging via `AddJsonConsole()`
- Repository operations log to CloudWatch with correlation to DynamoDB operations

## AWS Region

Default region is `us-east-2` (Ohio) in code, but GitHub Actions deploys to `us-east-1` (N. Virginia). Verify region consistency when deploying or debugging.
