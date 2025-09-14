using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using AICalendar.MCP.Services;

namespace AICalendar.MCP;

internal class Program
{
    static async Task Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);
        
        // Add Aspire service defaults
        builder.AddServiceDefaults();

        // CRITICAL: Clear all default logging and configure only stderr logging
        builder.Logging.ClearProviders();
        builder.Logging.AddConsole(consoleLogOptions =>
        {
            // Force ALL logs to stderr
            consoleLogOptions.LogToStandardErrorThreshold = LogLevel.Trace;
        });

        // Add HTTP client for calling Calendar API
        builder.Services.AddHttpClient("CalendarApi", client =>
        {
            // This will be configured via Aspire service discovery
            var baseUrl = builder.Configuration["CalendarApi:BaseUrl"] ?? "https://localhost:7242";
            client.BaseAddress = new Uri(baseUrl);
        });

        // Register CalendarTools service - MUST be registered for DI
        builder.Services.AddScoped<CalendarTools>();

        // Configure MCP Server
        builder.Services
                 .AddMcpServer()
                 .WithStdioServerTransport()
                 .WithToolsFromAssembly();

        var host = builder.Build();
        
        var logger = host.Services.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("AICalendar MCP Server starting...");


        // Start the MCP server
        await host.RunAsync();
    }
}
