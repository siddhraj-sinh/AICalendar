var builder = DistributedApplication.CreateBuilder(args);

// Add SQL Server
var sql = builder.AddSqlServer("sql")
    .WithLifetime(ContainerLifetime.Persistent);

var db = sql.AddDatabase("AICalendarDb");

// Add the API project first (dependency)
var apiService = builder.AddProject<Projects.AICalendar_Api>("aicalendar-api")
    .WithReference(db);

// Add Client with reference to API (Client calls API)
builder.AddProject<Projects.AICalendar_Client>("aicalendar-client")
    .WithReference(apiService);

// Add MCP console application with reference to API (MCP may call API)
builder.AddProject<Projects.AICalendar_MCP>("aicalendar-mcp")
    .WithReference(apiService);

var app = builder.Build();

app.Run();