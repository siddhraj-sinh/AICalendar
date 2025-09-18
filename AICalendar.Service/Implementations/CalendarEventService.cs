using AICalendar.DomainModels.DTOs;
using AICalendar.Service.Contracts;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

namespace AICalendar.Service.Implementations
{
    public class CalendarEventService : ICalendarEeventService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<CalendarEventService> _logger;

        public CalendarEventService(IHttpClientFactory httpClientFactory, IConfiguration configuration, IHttpContextAccessor httpContextAccessor, ILogger<CalendarEventService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        // Pass the access token from the client as a parameter
        public async Task<List<CalendarEventDto>> GetUserEventsAsync(DateTime? start, DateTime? end)
        {
            try
            {
                var clientAccessToken = GetAccessToken();
                // 1. Build confidential client for OBO
                var confidentialClient = ConfidentialClientApplicationBuilder
                    .Create(_configuration["AzureAd:ClientId"])       // CalendarApi ClientId
                    .WithClientSecret(_configuration["AzureAd:ClientSecret"]) // CalendarApi ClientSecret
                    .WithTenantId(_configuration["AzureAd:TenantId"])
                    .Build();

                // 2. UserAssertion from client token
                var userAssertion = new UserAssertion(clientAccessToken);

                // 3. Acquire token for Graph
                var result = await confidentialClient
                    .AcquireTokenOnBehalfOf(new[] { "https://graph.microsoft.com/.default" }, userAssertion)
                    .ExecuteAsync();

                var graphToken = result.AccessToken;

                // 4. Call Graph API using new token
                var client = _httpClientFactory.CreateClient();
                client.BaseAddress = new Uri("https://graph.microsoft.com/v1.0/");
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", graphToken);

                // 5. Build query using the new method
                var url = BuildQueryUrl(start, end);

                var response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();

                var events = new List<CalendarEventDto>();
                using var doc = JsonDocument.Parse(json);
                foreach (var item in doc.RootElement.GetProperty("value").EnumerateArray())
                {
                    events.Add(new CalendarEventDto
                    {
                        Id = item.GetProperty("id").GetString() ?? "",
                        Title = item.GetProperty("subject").GetString() ?? "",
                        Start = DateTime.Parse(item.GetProperty("start").GetProperty("dateTime").GetString() ?? ""),
                        End = DateTime.Parse(item.GetProperty("end").GetProperty("dateTime").GetString() ?? ""),
                        Description = item.TryGetProperty("bodyPreview", out var desc) ? desc.GetString() : null
                    });
                }
                return events;

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving calendar events");
                Console.WriteLine(ex.Message);
                throw;
            }
        }

        private string BuildQueryUrl(DateTime? start, DateTime? end)
        {
            var startDate = start ?? DateTime.Today;
            var endDate = end ?? DateTime.Today.AddDays(30);

            // Use CalendarView for date range queries (Microsoft recommended approach)
            if (start.HasValue || end.HasValue)
            {
                var startParam = Uri.EscapeDataString(startDate.ToString("yyyy-MM-ddTHH:mm:ss.fffK"));
                var endParam = Uri.EscapeDataString(endDate.ToString("yyyy-MM-ddTHH:mm:ss.fffK"));
                var select = "id,subject,start,end,bodyPreview,location,attendees,organizer";

                return $"me/calendarview?startDateTime={startParam}&endDateTime={endParam}&$select={select}&$orderby=start/dateTime&$top=100";
            }

            // Fallback to regular events endpoint if no date filtering needed
            return "me/events?$select=id,subject,start,end,bodyPreview,location,attendees,organizer&$orderby=start/dateTime&$top=100";
        }

        private string? GetAccessToken()
        {
            var authHeader = _httpContextAccessor.HttpContext?
                .Request.Headers["Authorization"].FirstOrDefault();

            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
                return null;

            return authHeader.Substring("Bearer ".Length).Trim();
        }
    }
}