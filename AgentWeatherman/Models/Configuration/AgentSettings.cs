using System.ComponentModel.DataAnnotations;

namespace AgentWeatherman.Models.Configuration;

/// <summary>
/// Configuration settings for the Weather Agent
/// </summary>
public class AgentSettings
{
    /// <summary>
    /// GitHub token for accessing GitHub Models API
    /// </summary>
    [Required]
    public string GitHubToken { get; set; } = string.Empty;

    /// <summary>
    /// GitHub Models API endpoint
    /// </summary>
    public string GitHubModelsEndpoint { get; set; } = "https://models.inference.ai.azure.com";

    /// <summary>
    /// Model name to use (e.g., "gpt-4o-mini", "Phi-3-mini-4k-instruct")
    /// </summary>
    public string ModelName { get; set; } = "gpt-4o-mini";

    /// <summary>
    /// MCP server WebSocket URL
    /// </summary>
    [Required]
    public string McpServerUrl { get; set; } = string.Empty;

    /// <summary>
    /// Maximum tokens for LLM responses
    /// </summary>
    public int MaxTokens { get; set; } = 1000;

    /// <summary>
    /// Temperature setting for LLM (0.0 - 1.0)
    /// </summary>
    public double Temperature { get; set; } = 0.7;
}