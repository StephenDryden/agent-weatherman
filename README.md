# Weather Agent 🌤️

A sophisticated AI-powered weather agent built in C# that combines GitHub Models LLM with MCP (Model Context Protocol) server for real-time weather data. The agent acts as a friendly weatherman, providing conversational weather forecasts and insights.

## Features

- 🤖 **AI-Powered Conversations**: Uses GitHub Models API (GPT-4o-mini) for natural language interactions
- 🌦️ **Real-time Weather Data**: Connects to MCP server for current weather and forecasts
- 🐳 **Docker Ready**: Containerized for easy deployment to AWS ECS or any container platform
- ⚡ **Minimal Configuration**: Simple setup with environment variables
- 📱 **Interactive Console**: Chat-like interface for weather inquiries
- 🔗 **MCP Protocol**: Standards-based communication with weather data sources

## Architecture

```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│  Weather Agent  │───▶│  GitHub Models  │    │   MCP Server    │
│   (Console UI)  │    │   LLM API       │    │ (Weather Data)  │
└─────────────────┘    └─────────────────┘    └─────────────────┘
         │                        │                        │
         └────────────────────────┼────────────────────────┘
                                  │
                            WebSocket/HTTP
```

## Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker](https://docs.docker.com/get-docker/) (for containerization)
- [GitHub Personal Access Token](https://github.com/settings/tokens) with appropriate permissions
- MCP Weather Server running on WebSocket (default: `ws://localhost:3000`)

## Quick Start

### 1. Clone and Build

```bash
git clone https://github.com/StephenDryden/agent-weatherman.git
cd agent-weatherman
make all  # or: dotnet build && docker build -t agent-weatherman .
```

### 2. Configuration

Set your GitHub token:
```bash
export GITHUB_TOKEN="your-github-personal-access-token"
```

### 3. Run the Agent

#### Option A: Run Locally
```bash
make run
# or manually:
export WEATHERAGENT_AgentSettings__GitHubToken="$GITHUB_TOKEN"
export WEATHERAGENT_AgentSettings__McpServerUrl="ws://localhost:3000"
dotnet run --project AgentWeatherman/AgentWeatherman.csproj
```

#### Option B: Run with Docker
```bash
make docker-build
make docker-run-local
```

## Configuration

The agent can be configured via environment variables or `appsettings.json`:

### Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `WEATHERAGENT_AgentSettings__GitHubToken` | GitHub personal access token | Required |
| `WEATHERAGENT_AgentSettings__McpServerUrl` | MCP server WebSocket URL | `ws://localhost:3000` |
| `WEATHERAGENT_AgentSettings__ModelName` | GitHub Models model name | `gpt-4o-mini` |
| `WEATHERAGENT_AgentSettings__MaxTokens` | Maximum tokens per response | `1000` |
| `WEATHERAGENT_AgentSettings__Temperature` | LLM temperature (0.0-1.0) | `0.7` |

### appsettings.json

```json
{
  "AgentSettings": {
    "GitHubToken": "your-token-here",
    "GitHubModelsEndpoint": "https://models.inference.ai.azure.com",
    "ModelName": "gpt-4o-mini",
    "McpServerUrl": "ws://localhost:3000",
    "MaxTokens": 1000,
    "Temperature": 0.7
  }
}
```

## Usage Examples

Once running, you can interact with the weather agent naturally:

```
You: What's the weather like in London today?

🌤️ Weather Agent: Good morning! Let me check the current weather in London for you.

Looking at the conditions right now, London is experiencing partly cloudy skies with a temperature of 18°C (64°F). There's a gentle breeze from the southwest at about 12 km/h, and humidity is sitting at a comfortable 65%.

For today, you can expect the temperature to reach a pleasant 22°C (72°F) this afternoon with the clouds gradually clearing. It's a great day for outdoor activities! I'd recommend a light jacket for the morning, but you should be comfortable in just a t-shirt by midday.

Would you like me to check the forecast for the next few days as well?
```

## Docker Deployment

### Build and Run

```bash
# Build the Docker image
make docker-build

# Run with local MCP server
make docker-run-local

# Run with remote MCP server
MCP_SERVER_URL="ws://your-mcp-server:3000" make docker-run
```

### AWS ECR Deployment

```bash
# Set AWS credentials and region
export AWS_ACCOUNT_ID="123456789012"
export AWS_REGION="us-west-2"

# Login to ECR and push
make ecr-login
make ecr-push
```

## MCP Server Integration

The agent expects an MCP server that provides weather tools. The server should implement:

### Required Tools

1. **get_current_weather**
   - Parameters: `location` (string)
   - Returns: Current weather conditions

2. **get_weather_forecast**
   - Parameters: `location` (string), `days` (number)
   - Returns: Multi-day weather forecast

### MCP Protocol

The agent communicates using standard MCP protocol over WebSocket:

```json
{
  "jsonrpc": "2.0",
  "id": "unique-id",
  "method": "tools/call",
  "params": {
    "name": "get_current_weather",
    "arguments": {
      "location": "London, UK"
    }
  }
}
```

## Development

### Project Structure

```
AgentWeatherman/
├── Models/
│   ├── Configuration/     # Settings and configuration
│   ├── GitHub/           # GitHub Models API DTOs
│   └── Mcp/             # MCP protocol models
├── Services/
│   ├── GitHubLlmService.cs      # GitHub Models API client
│   ├── McpClientService.cs      # MCP protocol client
│   └── WeatherAgentService.cs   # Main agent orchestrator
├── Program.cs           # Application entry point
└── appsettings.json    # Default configuration
```

### Available Make Commands

```bash
make help              # Show all available commands
make build             # Build the solution
make run               # Run locally
make docker-build      # Build Docker image
make docker-run-local  # Run container with local MCP server
make clean             # Clean build artifacts
make test              # Run tests
```

### Adding New Features

1. **New LLM Models**: Update `ModelName` in configuration
2. **Additional Weather Tools**: Extend `McpClientService` with new tool methods
3. **Enhanced Conversations**: Modify the system prompt in `WeatherAgentService`

## Troubleshooting

### Common Issues

1. **GitHub API Authentication Error**
   - Verify your GitHub token has correct permissions
   - Check token hasn't expired

2. **MCP Server Connection Failed**
   - Ensure MCP server is running and accessible
   - Verify WebSocket URL format: `ws://host:port`

3. **Docker Network Issues**
   - Use `--network host` for local MCP server
   - Ensure proper container networking for remote servers

### Logging

The application uses structured logging. Set log level via:

```bash
export DOTNET_ENVIRONMENT="Development"  # Verbose logging
# or
export Logging__LogLevel__Default="Debug"
```

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests if applicable
5. Submit a pull request

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Support

For issues and questions:
- Create an issue on GitHub
- Check the troubleshooting section above
- Review the MCP server documentation for integration details