using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AICalendar.MCP;

internal class Program
{
    static async Task Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);
        
        // Add Aspire service defaults
        builder.AddServiceDefaults();
        
        // Add MCP services
        builder.Services.AddScoped<IMcpService, McpService>();
        
        var host = builder.Build();
        
        Console.WriteLine("AICalendar MCP Service starting...");
        
        await host.RunAsync();
    }
}

// Placeholder interfaces and classes - implement these based on your MCP requirements
public interface IMcpService
{   
    Task ProcessAsync(string command);
}

public class McpService : IMcpService
{
    public async Task ProcessAsync(string command)
    {
        Console.WriteLine($"Processing MCP command: {command}");
        await Task.Delay(100); // Simulate processing
    }
}
