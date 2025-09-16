var builder = DistributedApplication.CreateBuilder(args);

// Add SQL Server
var sql = builder.AddSqlServer("sql")
    .WithLifetime(ContainerLifetime.Persistent);

var db = sql.AddDatabase("AICalendarDb");

// Add the API project first (dependency)
var apiService = builder.AddProject<Projects.AICalendar_Api>("aicalendar-backend-main-server")
    .WithReference(db);

// Add MCP console application with reference to API (MCP may call API)
var mcpServer = builder.AddProject<Projects.AICalendar_MCP>("aicalendar-backend-mcp-server")
    .WithReference(apiService);

var llmApiService = builder.AddProject<Projects.AICalendar_Chat>("aicalendar-backend-chat-server")
        .WithReference(mcpServer);

// Add Client with reference to API (Client calls API)
var clientService = builder.AddProject<Projects.AICalendar_Client>("aicalendar-frontend")
    .WithReference(apiService)
    .WithReference(llmApiService);




var app = builder.Build();

app.Run();