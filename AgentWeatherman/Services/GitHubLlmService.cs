using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using AgentWeatherman.Models.Configuration;
using AgentWeatherman.Models.GitHub;

namespace AgentWeatherman.Services;

/// <summary>
/// Service for interacting with GitHub Models API
/// </summary>
public class GitHubLlmService
{
    private readonly HttpClient _httpClient;
    private readonly AgentSettings _settings;
    private readonly ILogger<GitHubLlmService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public GitHubLlmService(
        HttpClient httpClient,
        IOptions<AgentSettings> settings,
        ILogger<GitHubLlmService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
        
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            WriteIndented = false
        };

        // Configure HTTP client
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_settings.GitHubToken}");
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "AgentWeatherman/1.0");
    }

    /// <summary>
    /// Send a chat completion request to GitHub Models API
    /// </summary>
    /// <param name="messages">List of chat messages</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>AI response</returns>
    public async Task<string> GetChatCompletionAsync(
        List<ChatMessage> messages, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new GitHubModelsRequest
            {
                Model = _settings.ModelName,
                Messages = messages,
                MaxTokens = _settings.MaxTokens,
                Temperature = _settings.Temperature
            };

            var json = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _logger.LogDebug("Sending request to GitHub Models API: {Request}", json);

            var response = await _httpClient.PostAsync(
                $"{_settings.GitHubModelsEndpoint}/chat/completions",
                content,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("GitHub Models API error: {StatusCode} - {Content}", 
                    response.StatusCode, errorContent);
                throw new HttpRequestException($"GitHub Models API error: {response.StatusCode}");
            }

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogDebug("Received response from GitHub Models API: {Response}", responseJson);

            var apiResponse = JsonSerializer.Deserialize<GitHubModelsResponse>(responseJson, _jsonOptions);

            if (apiResponse?.Choices?.Count > 0)
            {
                var aiResponse = apiResponse.Choices[0].Message.Content;
                _logger.LogInformation("AI Response received, tokens used: {TotalTokens}", 
                    apiResponse.Usage?.TotalTokens ?? 0);
                return aiResponse;
            }

            throw new InvalidOperationException("No response content received from GitHub Models API");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling GitHub Models API");
            throw;
        }
    }

    /// <summary>
    /// Create a chat message
    /// </summary>
    /// <param name="role">Message role (system, user, assistant)</param>
    /// <param name="content">Message content</param>
    /// <returns>Chat message</returns>
    public static ChatMessage CreateMessage(string role, string content)
    {
        return new ChatMessage
        {
            Role = role,
            Content = content
        };
    }
}