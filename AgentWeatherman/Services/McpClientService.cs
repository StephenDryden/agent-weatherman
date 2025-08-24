using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using AgentWeatherman.Models.Configuration;
using AgentWeatherman.Models.Mcp;

namespace AgentWeatherman.Services;

/// <summary>
/// Service for communicating with MCP (Model Context Protocol) server
/// </summary>
public class McpClientService : IDisposable
{
    private readonly AgentSettings _settings;
    private readonly ILogger<McpClientService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private ClientWebSocket? _webSocket;
    private readonly CancellationTokenSource _cancellationTokenSource;

    public McpClientService(
        IOptions<AgentSettings> settings,
        ILogger<McpClientService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
        _cancellationTokenSource = new CancellationTokenSource();
        
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    /// <summary>
    /// Connect to the MCP server
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _webSocket = new ClientWebSocket();
            var uri = new Uri(_settings.McpServerUrl);
            
            _logger.LogInformation("Connecting to MCP server at {Url}", _settings.McpServerUrl);
            
            await _webSocket.ConnectAsync(uri, cancellationToken);
            
            _logger.LogInformation("Successfully connected to MCP server");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to MCP server");
            throw;
        }
    }

    /// <summary>
    /// Send a tool call request to the MCP server
    /// </summary>
    /// <param name="toolName">Name of the tool to call</param>
    /// <param name="arguments">Tool arguments</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Tool result</returns>
    public async Task<McpToolResult> CallToolAsync(
        string toolName,
        Dictionary<string, object> arguments,
        CancellationToken cancellationToken = default)
    {
        if (_webSocket?.State != WebSocketState.Open)
        {
            throw new InvalidOperationException("WebSocket is not connected to MCP server");
        }

        try
        {
            var request = new McpRequest
            {
                Method = "tools/call",
                Params = new McpToolCallParams
                {
                    Name = toolName,
                    Arguments = arguments
                }
            };

            var requestJson = JsonSerializer.Serialize(request, _jsonOptions);
            var requestBytes = Encoding.UTF8.GetBytes(requestJson);

            _logger.LogDebug("Sending MCP tool call: {Tool} with args: {Args}", toolName, 
                JsonSerializer.Serialize(arguments));

            await _webSocket.SendAsync(
                new ArraySegment<byte>(requestBytes),
                WebSocketMessageType.Text,
                true,
                cancellationToken);

            // Receive response
            var buffer = new byte[8192];
            var result = await _webSocket.ReceiveAsync(
                new ArraySegment<byte>(buffer),
                cancellationToken);

            var responseJson = Encoding.UTF8.GetString(buffer, 0, result.Count);
            _logger.LogDebug("Received MCP response: {Response}", responseJson);

            var response = JsonSerializer.Deserialize<McpResponse>(responseJson, _jsonOptions);

            if (response?.Error != null)
            {
                _logger.LogError("MCP server error: {Code} - {Message}", 
                    response.Error.Code, response.Error.Message);
                throw new InvalidOperationException($"MCP server error: {response.Error.Message}");
            }

            if (response?.Result != null)
            {
                var toolResult = JsonSerializer.Deserialize<McpToolResult>(
                    JsonSerializer.Serialize(response.Result), _jsonOptions);
                return toolResult ?? new McpToolResult();
            }

            throw new InvalidOperationException("No result received from MCP server");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling MCP tool: {Tool}", toolName);
            throw;
        }
    }

    /// <summary>
    /// Get weather forecast for a location
    /// </summary>
    /// <param name="location">Location name or coordinates</param>
    /// <param name="days">Number of days to forecast (default: 3)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Weather forecast as text</returns>
    public async Task<string> GetWeatherForecastAsync(
        string location,
        int days = 3,
        CancellationToken cancellationToken = default)
    {
        var arguments = new Dictionary<string, object>
        {
            ["location"] = location,
            ["days"] = days
        };

        var result = await CallToolAsync("get_weather_forecast", arguments, cancellationToken);

        if (result.IsError)
        {
            throw new InvalidOperationException($"Weather forecast error: {string.Join(", ", result.Content.Select(c => c.Text))}");
        }

        return string.Join("\n", result.Content.Select(c => c.Text));
    }

    /// <summary>
    /// Get current weather for a location
    /// </summary>
    /// <param name="location">Location name or coordinates</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Current weather as text</returns>
    public async Task<string> GetCurrentWeatherAsync(
        string location,
        CancellationToken cancellationToken = default)
    {
        var arguments = new Dictionary<string, object>
        {
            ["location"] = location
        };

        var result = await CallToolAsync("get_current_weather", arguments, cancellationToken);

        if (result.IsError)
        {
            throw new InvalidOperationException($"Current weather error: {string.Join(", ", result.Content.Select(c => c.Text))}");
        }

        return string.Join("\n", result.Content.Select(c => c.Text));
    }

    /// <summary>
    /// Disconnect from the MCP server
    /// </summary>
    public async Task DisconnectAsync()
    {
        if (_webSocket?.State == WebSocketState.Open)
        {
            _logger.LogInformation("Disconnecting from MCP server");
            await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
        }
    }

    public void Dispose()
    {
        _cancellationTokenSource.Cancel();
        _webSocket?.Dispose();
        _cancellationTokenSource.Dispose();
    }
}