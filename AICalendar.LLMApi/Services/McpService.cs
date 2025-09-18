using ModelContextProtocol.Client;

namespace AICalendar.Chat.Services
{
    public class McpService
    {
        private IMcpClient? _client;
        private readonly ILogger<McpService> _logger;

        public McpService(ILogger<McpService> logger)
        {
            _logger = logger;
        }

        public async Task<IMcpClient> GetClientAsync()
        {
            if (_client == null)
            {
                try
                {
                    _logger.LogInformation("Attempting to start MCP server...");

                    // First verify the project path exists
                    var projectPath = Path.Combine(Environment.CurrentDirectory, "../AICalendar.MCP/AICalendar.MCP.csproj");
                    var fullPath = Path.GetFullPath(projectPath);
                    
                    if (!File.Exists(fullPath))
                    {
                        _logger.LogError("MCP project file not found at: {FullPath}", fullPath);
                        throw new FileNotFoundException($"MCP project file not found at: {fullPath}");
                    }

                    _client = await McpClientFactory.CreateAsync(
                        new StdioClientTransport(new()
                        {
                            Command = "dotnet",
                            Arguments = ["run", "--project", "../AICalendar.MCP/AICalendar.MCP.csproj"],
                            WorkingDirectory = Environment.CurrentDirectory,
                            Name = "AICalendar MCP Server"
                        }));

                    _logger.LogInformation("MCP client initialized successfully");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to initialize MCP client. Error: {ErrorMessage}", ex.Message);
                    
                    // Log additional details for debugging
                    if (ex.InnerException != null)
                    {
                        _logger.LogError("Inner exception: {InnerException}", ex.InnerException.Message);
                    }
                    
                    throw;
                }
            }

            return _client;
        }

       
    }
}
