using ChessOfCards.LocalTestServer;

var builder = WebApplication.CreateBuilder(args);

// Configure environment variables for local testing
Environment.SetEnvironmentVariable("ENVIRONMENT_NAME", "dev");
Environment.SetEnvironmentVariable("CONNECTIONS_TABLE_NAME", "chess-of-cards-connections-dev");
Environment.SetEnvironmentVariable("PENDING_GAMES_TABLE_NAME", "chess-of-cards-pending-games-dev");
Environment.SetEnvironmentVariable("ACTIVE_GAMES_TABLE_NAME", "chess-of-cards-active-games-dev");
Environment.SetEnvironmentVariable("GAME_TIMERS_TABLE_NAME", "chess-of-cards-game-timers-dev");
Environment.SetEnvironmentVariable("WEBSOCKET_ENDPOINT", "http://localhost:3001");
Environment.SetEnvironmentVariable("IS_LOCAL_TESTING", "true");

// Add services
builder.Services.AddSingleton<LocalWebSocketManager>();
builder.Services.AddScoped<LocalWebSocketServiceAdapter>();
builder.Services.AddScoped<WebSocketHandler>();

// Configure CORS for local development
builder.Services.AddCors(options =>
{
    options.AddPolicy(
        "AllowAll",
        policy =>
        {
            policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
        }
    );
});

var app = builder.Build();

// Enable WebSockets
app.UseWebSockets(new WebSocketOptions { KeepAliveInterval = TimeSpan.FromMinutes(2) });

app.UseCors("AllowAll");

// WebSocket endpoint
app.Map(
    "/",
    async context =>
    {
        if (context.WebSockets.IsWebSocketRequest)
        {
            var handler = context.RequestServices.GetRequiredService<WebSocketHandler>();
            await handler.HandleWebSocketAsync(context);
        }
        else
        {
            context.Response.StatusCode = 400;
            await context.Response.WriteAsync("WebSocket connection required");
        }
    }
);

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

// Info endpoint
app.MapGet(
    "/info",
    () =>
        Results.Ok(
            new
            {
                message = "Chess of Cards Local WebSocket Test Server",
                websocketUrl = "ws://localhost:3001",
                instructions = new[]
                {
                    "Connect to ws://localhost:3001 with your client",
                    "This server forwards requests to Lambda functions locally",
                    "Uses dev DynamoDB tables for testing",
                },
            }
        )
);

app.Logger.LogInformation("Starting Chess of Cards Local Test Server...");
app.Logger.LogInformation("WebSocket URL: ws://localhost:3001");
app.Logger.LogInformation("Health Check: http://localhost:3001/health");
app.Logger.LogInformation("Environment: dev");

app.Run("http://localhost:3001");
