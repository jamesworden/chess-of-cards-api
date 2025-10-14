# Debugging Guide

This guide explains how to debug the Chess of Cards API locally in VS Code.

## Quick Start

### Option 1: VS Code Debugger (Recommended)

1. **Press F5** or go to Run & Debug panel (Ctrl+Shift+D)
2. Select **"Debug Local Test Server (Watch)"** from the dropdown
3. Click the green play button or press F5
4. Set breakpoints in your code
5. Make requests from your frontend - the debugger will stop at breakpoints

### Option 2: Attach to Running Process

If you already have the server running via `npm run start:watch`:

1. Go to Run & Debug panel (Ctrl+Shift+D)
2. Select **"Attach to Local Test Server"** from the dropdown
3. Click the green play button
4. VS Code will attach to the running process

### Option 3: npm Script with Manual Attach

1. Run in terminal: `npm run start:debug`
2. Wait for server to start
3. Attach debugger using **"Attach to Local Test Server"** (see Option 2)

## Available Debug Configurations

### Debug Local Test Server
- **Use when**: You want to debug without watch mode
- **How**: Press F5 → Select "Debug Local Test Server"
- **Features**: Runs once, stops when you stop debugging

### Debug Local Test Server (Watch)
- **Use when**: You want code changes to auto-reload while debugging
- **How**: Press F5 → Select "Debug Local Test Server (Watch)"
- **Features**: Auto-reloads on code changes, maintains debug session

### Attach to Local Test Server
- **Use when**: Server is already running (via npm or PowerShell script)
- **How**: Press F5 → Select "Attach to Local Test Server"
- **Features**: Attaches to existing process without restarting

## Setting Breakpoints

1. Open any C# file (e.g., a command handler)
2. Click in the gutter (left of line numbers) to set a breakpoint
3. Red dot appears when breakpoint is set
4. When code executes, debugger will pause at breakpoint

## Common Debugging Scenarios

### Debug Game Join Logic
```csharp
// Set breakpoint in JoinGameCommandHandler.cs
public async Task Handle(JoinGameCommand command, CancellationToken cancellationToken)
{
    // Breakpoint here ← Click in gutter
    _logger.LogInformation($"Player joining game {command.GameCode}");
    ...
}
```

### Debug Move Validation
```csharp
// Set breakpoint in MakeMoveCommandHandler.cs
public async Task Handle(MakeMoveCommand command, CancellationToken cancellationToken)
{
    // Breakpoint here ← Click in gutter
    var activeGameRecord = await _activeGameRepository.GetByConnectionIdAsync(
        command.ConnectionId
    );
    ...
}
```

### Debug WebSocket Messages
```csharp
// Set breakpoint in WebSocketService.cs
public virtual async Task<bool> SendMessageAsync(string connectionId, object message)
{
    // Breakpoint here ← Click in gutter to see all messages
    var json = JsonSerializer.Serialize(message, JsonOptions);
    ...
}
```

## Debugger Controls

- **F5**: Continue execution
- **F10**: Step over (execute current line, don't step into functions)
- **F11**: Step into (step into function calls)
- **Shift+F11**: Step out (finish current function and return)
- **Ctrl+Shift+F5**: Restart debugger
- **Shift+F5**: Stop debugging

## Debug Console

While debugging, you can:
- **Inspect variables**: Hover over variables to see their values
- **Watch expressions**: Add expressions to the Watch panel
- **Evaluate expressions**: Use the Debug Console to evaluate C# expressions
- **View call stack**: See the sequence of method calls that led to current point

## Troubleshooting

### Can't attach to process
- Make sure the LocalTestServer is running (`npm run start:watch`)
- Check Task Manager for ChessOfCards.LocalTestServer.exe
- Try restarting the server

### Breakpoints not hitting
- Verify you're running in Debug mode (not Release)
- Check that the code you're debugging is actually being executed
- Try rebuilding the solution: `npm run build`

### Debugger slow or freezing
- Reduce number of active breakpoints
- Disable "Break on exceptions" if enabled
- Close other applications to free up memory

## Tips & Tricks

1. **Conditional Breakpoints**: Right-click breakpoint → Edit Breakpoint → Add condition
   - Example: `command.GameCode == "ABC123"` (only breaks for specific game)

2. **Log Points**: Right-click → Add Logpoint (logs without stopping)
   - Example: `Player {command.ConnectionId} joining game {command.GameCode}`

3. **Watch Window**: Add variables to watch their values change
   - Right-click variable → Add to Watch

4. **Immediate Window**: Evaluate expressions and call methods during debugging
   - Press Ctrl+Shift+Y to open

## Related Documentation

- [Local Testing Guide](./LOCAL_TESTING.md)
- [Quick Start Guide](./QUICK_START.md)
- [VS Code Debugging Docs](https://code.visualstudio.com/docs/editor/debugging)
