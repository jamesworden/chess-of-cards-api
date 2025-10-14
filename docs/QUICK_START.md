# Quick Start Guide

## 🚀 Running Locally (3 Options)

### Option 1: VS Code Task (Easiest!)
1. Press **`Ctrl+Shift+P`** (Windows/Linux) or **`Cmd+Shift+P`** (Mac)
2. Type **"Run Task"**
3. Select **"Run Local Test Server"**

✅ Server starts on `http://localhost:5000`
✅ WebSocket endpoint: `ws://localhost:5000/ws`

---

### Option 2: PowerShell Script
```powershell
.\scripts\run-local-server.ps1
```

**With options:**
```powershell
.\scripts\run-local-server.ps1 -Port 8080      # Custom port
.\scripts\run-local-server.ps1 -Rebuild        # Rebuild first
.\scripts\run-local-server.ps1 -Watch          # Auto-reload on changes
```

---

### Option 3: Manual
```bash
cd tools/ChessOfCards.LocalTestServer
dotnet run
```

---

## 🧪 Testing with wscat

```bash
# Install wscat (one time)
npm install -g wscat

# Connect
wscat -c ws://localhost:5000/ws

# Send a message
{"action":"createPendingGame","data":{"hostName":"Alice"}}
```

---

## 🔨 Other Useful VS Code Tasks

Press **`Ctrl+Shift+P`** → **"Run Task"** → Select:

- **Build Solution** - `dotnet build ChessOfCards.sln`
- **Run Tests** - `dotnet test ChessOfCards.sln`
- **SAM Build** - Build Lambda deployment packages
- **SAM Local Start API** - Start with SAM CLI
- **SAM Deploy (Dev)** - Deploy to AWS dev environment

---

## 📝 Configuration Files

- **PowerShell Script**: `run-local-test-server.ps1`
- **VS Code Tasks**: `.vscode/tasks.json`
- **Local Test Server**: `tools/ChessOfCards.LocalTestServer/`

---

## 📚 Full Documentation

- **Local Testing**: [LOCAL_TESTING.md](LOCAL_TESTING.md)
- **Deployment**: [DEPLOYMENT.md](DEPLOYMENT.md)
- **Architecture**: [SERVERLESS_ARCHITECTURE.md](SERVERLESS_ARCHITECTURE.md)
- **Project Status**: [PROJECT_STATUS.md](PROJECT_STATUS.md)
- **Best Practices**: [BEST_PRACTICES_TODO.md](BEST_PRACTICES_TODO.md)

---

## 🆘 Troubleshooting

**Port already in use?**
```powershell
.\scripts\run-local-server.ps1 -Port 8080
```

**Can't connect to DynamoDB?**
```bash
aws sts get-caller-identity  # Check AWS credentials
```

**Build errors?**
```bash
dotnet clean ChessOfCards.sln
dotnet build ChessOfCards.sln
```

---

## 🎯 Quick Commands Reference

| Command | Description |
|---------|-------------|
| `.\scripts\run-local-server.ps1` | Start local server |
| `dotnet build` | Build solution |
| `dotnet test` | Run all tests |
| `sam build` | Build Lambda packages |
| `sam local start-api` | Start SAM local |
| `sam deploy` | Deploy to AWS |

---

**Need more help?** See [LOCAL_TESTING.md](LOCAL_TESTING.md) for detailed documentation.
