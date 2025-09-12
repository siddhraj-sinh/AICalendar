using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Web.UI;
using Microsoft.Identity.Web;

namespace AICalendar.Client;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.AddServiceDefaults();

        // Sign-in and acquire tokens for your API
        builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
            .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"))
            .EnableTokenAcquisitionToCallDownstreamApi()
            .AddDownstreamApi("CalendarApi", builder.Configuration.GetSection("DownstreamApi:CalendarApi"))
            .AddInMemoryTokenCaches();

        builder.Services.AddRazorPages()
            .AddMicrosoftIdentityUI();

        builder.Services.AddAuthorization(options =>
        {
            // Require auth by default across pages (optional but nice)
            options.FallbackPolicy = options.DefaultPolicy;
        });


        var app = builder.Build();

        app.MapDefaultEndpoints();

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles();

        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapRazorPages();

        app.Run();
    }
}
