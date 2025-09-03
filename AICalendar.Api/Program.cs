using AICalendar.DomainModels.Context;
using Microsoft.Extensions.Hosting;

namespace AICalendar.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add Aspire service defaults
            builder.AddServiceDefaults();

            // Add services to the container.
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            // Configure SQL Server with Aspire
            builder.AddSqlServerDbContext<AppDbContext>("DefaultConnection");

            var app = builder.Build();

            // Map Aspire default endpoints
            app.MapDefaultEndpoints();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();
            app.UseAuthorization();
            app.MapControllers();

            app.Run();
        }
    }
}