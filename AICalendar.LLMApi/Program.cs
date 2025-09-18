using AICalendar.Chat.Services;
using AICalendar.LLMApi.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.AI;
using Microsoft.Identity.Web;
using OpenAI;
using OpenAI.Chat;
using System.ClientModel;

namespace AICalendar.LLMApi;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.AddServiceDefaults();

        /// Add JWT Authentication
        builder.Services.AddAuthentication(options =>
        {
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
.AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));

        //var apiKey = builder.Configuration["OpenAI:GithubApiKey"];
        //var model = builder.Configuration["OpenAI:Model"];
        //// Register OpenAI ChatClient as IChatClient
        //builder.Services.AddSingleton<IChatClient>(_ =>
        //{
        //    var client = new ChatClient(
        //        model: model,   
        //        apiKey: apiKey
        //    );
        //    return client.AsIChatClient();
        //});

        var githubToken = builder.Configuration["GitHub:Token"];

        var endpoint = new Uri("https://models.github.ai/inference");

        // Register ChatClient from GitHub Models as IChatClient
        builder.Services.AddSingleton<IChatClient>(_ =>
        {
            var client = new ChatClient(
                model: "openai/gpt-4.1", // GitHub catalog model name
                credential: new ApiKeyCredential(githubToken),
                options: new OpenAIClientOptions { Endpoint = endpoint }
            );

            return client.AsIChatClient(); // Adapt to IChatClient
        });

        // Register MCP service
        builder.Services.AddSingleton<McpService>();

        // Register ChatService for dependency injection
        builder.Services.AddScoped<IChatService, ChatService>();

        builder.Services.AddControllers();
        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();



        var app = builder.Build();

        app.MapDefaultEndpoints();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();
        app.UseAuthentication();
        app.UseAuthorization();


        app.MapControllers();

        app.Run();
    }
}
