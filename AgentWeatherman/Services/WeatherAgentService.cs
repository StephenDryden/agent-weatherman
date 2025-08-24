using Microsoft.Extensions.Logging;
using AgentWeatherman.Models.GitHub;

namespace AgentWeatherman.Services;

/// <summary>
/// Main weather agent that combines LLM and weather data capabilities
/// </summary>
public class WeatherAgentService
{
    private readonly GitHubLlmService _llmService;
    private readonly McpClientService _mcpService;
    private readonly ILogger<WeatherAgentService> _logger;
    private readonly List<ChatMessage> _conversationHistory;

    private const string SystemPrompt = @"You are a friendly and knowledgeable weather agent. Your role is to help users understand weather conditions and forecasts by:

1. Interpreting weather data in a conversational, easy-to-understand way
2. Providing context about what weather conditions mean for daily activities
3. Offering helpful suggestions based on the weather (e.g., what to wear, travel considerations)
4. Acting as a professional meteorologist who can explain weather patterns
5. Being personable and engaging while remaining accurate

When users ask about weather, you should:
- Get the current weather and/or forecast data from your weather tools
- Present the information in a natural, conversational way
- Add context about what the conditions mean
- Offer practical advice when relevant
- Ask clarifying questions if the location isn't specific enough

Always be helpful, accurate, and maintain a friendly, professional tone as if you're a TV weather person.";

    public WeatherAgentService(
        GitHubLlmService llmService,
        McpClientService mcpService,
        ILogger<WeatherAgentService> logger)
    {
        _llmService = llmService;
        _mcpService = mcpService;
        _logger = logger;
        _conversationHistory = new List<ChatMessage>
        {
            GitHubLlmService.CreateMessage("system", SystemPrompt)
        };
    }

    /// <summary>
    /// Initialize the weather agent (connect to MCP server)
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Initializing Weather Agent...");
        await _mcpService.ConnectAsync(cancellationToken);
        _logger.LogInformation("Weather Agent initialized successfully");
    }

    /// <summary>
    /// Process a user message and return the agent's response
    /// </summary>
    /// <param name="userMessage">User's message or question</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Agent's response</returns>
    public async Task<string> ProcessMessageAsync(string userMessage, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Processing user message: {Message}", userMessage);

            // Add user message to conversation history
            _conversationHistory.Add(GitHubLlmService.CreateMessage("user", userMessage));

            // Determine if we need weather data
            var needsWeatherData = await DetermineIfWeatherDataNeededAsync(userMessage, cancellationToken);

            string weatherContext = "";
            if (needsWeatherData.needed)
            {
                _logger.LogInformation("Weather data needed for location: {Location}", needsWeatherData.location);
                weatherContext = await GetWeatherContextAsync(needsWeatherData.location, cancellationToken);
            }

            // Create the enhanced prompt with weather context if available
            var enhancedMessage = userMessage;
            if (!string.IsNullOrEmpty(weatherContext))
            {
                enhancedMessage = $"{userMessage}\n\n[Current Weather Data for context:\n{weatherContext}]";
            }

            // Update the last user message with enhanced content
            _conversationHistory[_conversationHistory.Count - 1] = 
                GitHubLlmService.CreateMessage("user", enhancedMessage);

            // Get AI response
            var aiResponse = await _llmService.GetChatCompletionAsync(_conversationHistory, cancellationToken);

            // Add AI response to conversation history
            _conversationHistory.Add(GitHubLlmService.CreateMessage("assistant", aiResponse));

            // Keep conversation history manageable (last 10 messages + system prompt)
            if (_conversationHistory.Count > 11)
            {
                var systemMessage = _conversationHistory[0];
                var recentMessages = _conversationHistory.Skip(_conversationHistory.Count - 10).ToList();
                _conversationHistory.Clear();
                _conversationHistory.Add(systemMessage);
                _conversationHistory.AddRange(recentMessages);
            }

            _logger.LogInformation("Agent response generated successfully");
            return aiResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing user message");
            return "I apologize, but I'm having trouble processing your request right now. Please try again later.";
        }
    }

    /// <summary>
    /// Determine if the user's message requires weather data
    /// </summary>
    private async Task<(bool needed, string location)> DetermineIfWeatherDataNeededAsync(
        string userMessage, 
        CancellationToken cancellationToken)
    {
        var analysisPrompt = @"Analyze the following user message and determine:
1. Does this message require current weather data or forecast information? (yes/no)
2. If yes, what is the location mentioned? Extract the most specific location mentioned.

User message: """ + userMessage + @"""

Respond in this exact format:
NEEDS_WEATHER: [yes/no]
LOCATION: [extracted location or 'not specified']";

        var analysisMessages = new List<ChatMessage>
        {
            GitHubLlmService.CreateMessage("system", "You are a text analyzer that determines if weather data is needed."),
            GitHubLlmService.CreateMessage("user", analysisPrompt)
        };

        var analysis = await _llmService.GetChatCompletionAsync(analysisMessages, cancellationToken);

        var needsWeather = analysis.Contains("NEEDS_WEATHER: yes", StringComparison.OrdinalIgnoreCase);
        var location = "current location"; // Default location

        if (needsWeather)
        {
            var locationLine = analysis.Split('\n')
                .FirstOrDefault(line => line.StartsWith("LOCATION:", StringComparison.OrdinalIgnoreCase));
            
            if (locationLine != null)
            {
                var extractedLocation = locationLine.Substring("LOCATION:".Length).Trim();
                if (!extractedLocation.Equals("not specified", StringComparison.OrdinalIgnoreCase))
                {
                    location = extractedLocation;
                }
            }
        }

        return (needsWeather, location);
    }

    /// <summary>
    /// Get weather context for the AI
    /// </summary>
    private async Task<string> GetWeatherContextAsync(string location, CancellationToken cancellationToken)
    {
        try
        {
            var currentWeather = await _mcpService.GetCurrentWeatherAsync(location, cancellationToken);
            var forecast = await _mcpService.GetWeatherForecastAsync(location, 3, cancellationToken);

            return $"Current Weather for {location}:\n{currentWeather}\n\n3-Day Forecast:\n{forecast}";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not retrieve weather data for location: {Location}", location);
            return $"Weather data for {location} is currently unavailable.";
        }
    }

    /// <summary>
    /// Shutdown the weather agent
    /// </summary>
    public async Task ShutdownAsync()
    {
        _logger.LogInformation("Shutting down Weather Agent...");
        await _mcpService.DisconnectAsync();
        _mcpService.Dispose();
        _logger.LogInformation("Weather Agent shut down successfully");
    }
}