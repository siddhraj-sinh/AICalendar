var builder = DistributedApplication.CreateBuilder(args);

// Add SQL Server
var sql = builder.AddSqlServer("sql")
    .WithLifetime(ContainerLifetime.Persistent);

var db = sql.AddDatabase("AICalendarDb");

// Add the API project first (dependency)
var apiService = builder.AddProject<Projects.AICalendar_Api>("aicalendar-api")
    .WithReference(db);

// Add MCP console application with reference to API (MCP may call API)
var mcpServer = builder.AddProject<Projects.AICalendar_MCP>("aicalendar-mcp")
    .WithReference(apiService);

var llmApiService = builder.AddProject<Projects.AICalendar_LLMApi>("aicalendar-llmapi")
        .WithReference(mcpServer);

// Add Client with reference to API (Client calls API)
var clientService = builder.AddProject<Projects.AICalendar_Client>("aicalendar-client")
    .WithReference(apiService)
    .WithReference(llmApiService);




var app = builder.Build();

app.Run();