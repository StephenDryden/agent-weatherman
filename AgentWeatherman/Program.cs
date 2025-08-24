using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using AgentWeatherman.Models.Configuration;
using AgentWeatherman.Services;

namespace AgentWeatherman;

/// <summary>
/// Main entry point for the Weather Agent application
/// </summary>
public class Program
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("🌤️  Weather Agent Starting...");

        // Build the host
        var host = CreateHostBuilder(args).Build();

        try
        {
            // Get required services
            var weatherAgent = host.Services.GetRequiredService<WeatherAgentService>();
            var logger = host.Services.GetRequiredService<ILogger<Program>>();

            // Initialize the weather agent
            await weatherAgent.InitializeAsync();

            logger.LogInformation("Weather Agent is ready! Type 'quit' or 'exit' to stop.");

            Console.WriteLine("\n🌤️  Welcome to your Weather Agent!");
            Console.WriteLine("Ask me anything about the weather, and I'll help you out!");
            Console.WriteLine("Type 'quit' or 'exit' to stop.\n");

            // Main conversation loop
            while (true)
            {
                Console.Write("You: ");
                var userInput = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(userInput))
                    continue;

                if (userInput.Equals("quit", StringComparison.OrdinalIgnoreCase) ||
                    userInput.Equals("exit", StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }

                Console.Write("🌤️  Weather Agent: ");
                try
                {
                    var response = await weatherAgent.ProcessMessageAsync(userInput);
                    Console.WriteLine(response);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Sorry, I encountered an error processing your request. Please try again.");
                    logger.LogError(ex, "Error processing user input: {Input}", userInput);
                }

                Console.WriteLine();
            }

            // Shutdown
            await weatherAgent.ShutdownAsync();
            Console.WriteLine("\n👋 Goodbye! Stay weather-aware!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error starting Weather Agent: {ex.Message}");
            Console.WriteLine("Please check your configuration and try again.");
            return;
        }
    }

    /// <summary>
    /// Create and configure the host builder
    /// </summary>
    /// <param name="args">Command line arguments</param>
    /// <returns>Configured host builder</returns>
    private static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((context, config) =>
            {
                config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: false);
                config.AddEnvironmentVariables("WEATHERAGENT_");
                config.AddCommandLine(args);
            })
            .ConfigureServices((context, services) =>
            {
                // Configure settings
                services.Configure<AgentSettings>(context.Configuration.GetSection("AgentSettings"));

                // Register HTTP client
                services.AddHttpClient<GitHubLlmService>();

                // Register services
                services.AddSingleton<McpClientService>();
                services.AddSingleton<GitHubLlmService>();
                services.AddSingleton<WeatherAgentService>();
            })
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConsole(options =>
                {
                    options.LogToStandardErrorThreshold = LogLevel.Warning;
                });
                logging.SetMinimumLevel(LogLevel.Information);
            });
}
