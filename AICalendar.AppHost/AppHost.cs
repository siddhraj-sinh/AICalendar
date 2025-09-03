var builder = DistributedApplication.CreateBuilder(args);

// Add SQL Server
var sql = builder.AddSqlServer("sql")
    .WithLifetime(ContainerLifetime.Persistent);

var db = sql.AddDatabase("AICalendarDb");

// Add the API project using generated type (preferred method)
builder.AddProject<Projects.AICalendar_Api>("aicalendar-api")
    .WithReference(db);

var app = builder.Build();

app.Run();