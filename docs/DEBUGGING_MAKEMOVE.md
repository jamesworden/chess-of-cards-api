# Debugging MakeMoveCommandHandler - Step by Step Guide

This guide provides step-by-step instructions for debugging the `MakeMoveCommandHandler` in VS Code.

## Prerequisites

- VS Code with C# Dev Kit extension installed
- Solution built in Debug mode (already done)
- A WebSocket client to send test messages (wscat or your game client)

## Step 1: Start the Debugger

1. **Open VS Code** to this workspace
2. **Press F5** or click the Run icon in the sidebar (Ctrl+Shift+D)
3. **Select "Debug Local Test Server (Watch)"** from the dropdown at the top
4. **Press F5** or click the green play button

You should see:
```
Starting Chess of Cards Local Test Server...
WebSocket URL: ws://localhost:3001
Health Check: http://localhost:3001/health
Environment: dev
```

## Step 2: Set Breakpoints in MakeMoveCommandHandler

1. **Open the file**: `src/ChessOfCards.GameActionHandler.Application/Features/Games/Handlers/MakeMoveCommandHandler.cs`
2. **Click in the gutter** (left of line numbers) on the following lines to set breakpoints:
   - **Line 24**: Start of `Handle` method - to verify the handler is invoked
   - **Line 27**: After retrieving active game - to inspect game state
   - **Line 50**: Before executing move - to see the move details
   - **Line 118**: Before updating repository - to see final state

Red dots should appear indicating breakpoints are set.

## Step 3: Create a Game and Join

First, you need to have an active game. Use wscat or your client:

### Terminal 1 - Host Player
```bash
wscat -c ws://localhost:3001
Connected

# Send this message:
{"action": "createPendingGame", "playerName": "Alice"}

# You'll receive a game code, e.g., "ABC123"
```

### Terminal 2 - Guest Player
```bash
wscat -c ws://localhost:3001
Connected

# Send this message (use the game code from above):
{"action": "joinGame", "gameCode": "ABC123", "playerName": "Bob"}

# Both players should receive GameStarted message
```

## Step 4: Trigger the Breakpoint by Making a Move

In **Terminal 1** (Host Player - it's their turn first), send a move:

```json
{
  "action": "makeMove",
  "move": {
    "card": {"suit": "SPADES", "rank": "ACE"},
    "x": 0,
    "y": 0
  },
  "rearrangedCardsInHand": []
}
```

## Step 5: Debug Through the Code

When you send the move, **the debugger should pause** at your first breakpoint (line 24).

### What You Should See:

1. **VS Code pauses execution** - the line with the breakpoint is highlighted in yellow
2. **Variables panel** (on the left) shows:
   - `command` - the MakeMoveCommand with connectionId and move details
   - `cancellationToken` - the cancellation token
3. **Call Stack panel** shows the execution path

### Debugging Actions:

- **F10** (Step Over): Execute current line and move to next
- **F11** (Step Into): Step into method calls (like `game.MakeMove()`)
- **F5** (Continue): Run until next breakpoint
- **Shift+F5** (Stop): Stop debugging

### Key Points to Inspect:

#### At Line 27 (after retrieving game):
- Hover over `activeGameRecord` to see the game data
- Expand to see: `GameCode`, `HostConnectionId`, `GuestConnectionId`, `GameState`

#### At Line 50 (before executing move):
- Hover over `game` to see the deserialized Game object
- Hover over `command.Move` to see the move details
- Check `command.ConnectionId` matches the current player

#### At Line 118 (before updating):
- Check `activeGameRecord.GameState` - it should now contain updated game state
- Check `activeGameRecord.IsHostPlayersTurn` - should be toggled if move was valid
- Hover over `game` to see the updated game state

## Step 6: Verify Results

After the debugger continues (press F5), check the WebSocket clients:

### Both Players Should Receive:
```json
{
  "MessageType": "GameUpdated",
  "Data": {
    // Updated game view with new state
  }
}
```

## Execution Flow Summary

Here's the complete flow from WebSocket message to handler:

```
WebSocket Message (makeMove)
    ↓
WebSocketHandler.cs:139 - HandleMessageAsync()
    ↓
GameActionHandler.Function.cs:58 - ActionDispatcher.DispatchAsync()
    ↓
ActionDispatcher.cs:135 - HandleMakeMoveAsync()
    ↓
ActionDispatcher.cs:144 - _mediator.Publish()
    ↓
MakeMoveCommandHandler.cs:23 - Handle() ← YOUR BREAKPOINT HERE
```

## Troubleshooting

### Breakpoint Shows "Unverified Breakpoint" (hollow circle)
- **Cause**: Code hasn't been loaded yet or source doesn't match binary
- **Fix**:
  1. Stop debugger (Shift+F5)
  2. Clean and rebuild: `dotnet clean && dotnet build ChessOfCards.sln`
  3. Restart debugger (F5)

### Breakpoint Not Hitting
- **Verify move action is being sent**: Check the terminal shows `Action: makeMove` in logs
- **Verify game is active**: You must create and join a game first
- **Check it's the correct player's turn**: Only the current player's move will be processed
- **Verify breakpoint is in Debug build**: Check that `.dll` and `.pdb` files in `bin/Debug/` are up to date

### Can't See Variable Values
- **Enable "Just My Code" = false**: Already configured in launch.json
- **Rebuild in Debug mode**: `dotnet build -c Debug`
- **Check optimization**: Make sure you're not in Release mode

### "Source Not Available" Message
- **Verify source file path**: Make sure the file hasn't been moved
- **Check symbol files**: Look for `.pdb` files next to `.dll` files in `bin/Debug/`
- **Restart OmniSharp**: Press Ctrl+Shift+P → "OmniSharp: Restart OmniSharp"

## Advanced Debugging Techniques

### Conditional Breakpoints
Right-click on a breakpoint → **Edit Breakpoint** → **Add Condition**

Example conditions:
- `command.ConnectionId == "specific-connection-id"` - break only for specific player
- `command.Move.Card.Rank == "ACE"` - break only when moving an Ace
- `game.IsHostPlayersTurn == false` - break only on guest player's turn

### Log Points
Right-click in gutter → **Add Logpoint**

Example log message:
```
Move: {command.Move.Card.Rank} of {command.Move.Card.Suit} to ({command.Move.X}, {command.Move.Y})
```

This logs without stopping execution.

### Watch Expressions
In the **Watch** panel, add expressions to monitor:
- `game.IsHostPlayersTurn`
- `game.HasEnded`
- `results.Contains(MakeMoveResults.InvalidMove)`

## Testing Different Scenarios

### Test Invalid Move
```json
{
  "action": "makeMove",
  "move": {
    "card": {"suit": "INVALID", "rank": "ACE"},
    "x": 99,
    "y": 99
  },
  "rearrangedCardsInHand": []
}
```
Set breakpoint at line 57 to see it handle `InvalidMove` result.

### Test Wrong Player Turn
- Send a move from Terminal 2 (guest) immediately after host moves
- Breakpoint should hit but game won't update if it's not guest's turn

### Test Game Over Scenario
Set breakpoint at line 73 to catch when `game.HasEnded` is true.

## Common Issues with npm run start:debug

**Problem**: You ran `npm run start:debug` in the JavaScript Debug Terminal and breakpoints don't work.

**Why**: `npm run start:debug` starts a .NET process, but the JavaScript Debug Terminal tries to attach a Node.js debugger (wrong debugger type).

**Solution**: Don't use JavaScript Debug Terminal. Instead:
1. Use VS Code's debugger (F5)
2. OR use a regular PowerShell terminal for npm commands

## Next Steps

Once you've verified breakpoints work in `MakeMoveCommandHandler`, you can:
1. Debug other handlers (JoinGameCommandHandler, PassMoveCommandHandler, etc.)
2. Step into Domain logic (Game.cs, Move validation, etc.)
3. Debug WebSocket messaging in WebSocketService.cs
4. Test error scenarios and edge cases

## Related Files

- **Handler**: `src/ChessOfCards.GameActionHandler.Application/Features/Games/Handlers/MakeMoveCommandHandler.cs`
- **Command**: `src/ChessOfCards.GameActionHandler.Application/Features/Games/Commands/MakeMoveCommand.cs`
- **Domain Logic**: `src/ChessOfCards.Domain/Features/Games/Entities/Game/Game.cs`
- **Dispatcher**: `src/ChessOfCards.GameActionHandler/Handlers/ActionDispatcher.cs`
- **Test Server**: `tools/ChessOfCards.LocalTestServer/WebSocketHandler.cs`

## Quick Reference

| Action | Shortcut |
|--------|----------|
| Start Debugging | F5 |
| Stop Debugging | Shift+F5 |
| Restart Debugging | Ctrl+Shift+F5 |
| Step Over | F10 |
| Step Into | F11 |
| Step Out | Shift+F11 |
| Continue | F5 |
| Toggle Breakpoint | F9 |
| Run to Cursor | Ctrl+F10 |
