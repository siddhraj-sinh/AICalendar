using AICalendar.DomainModels.DTOs;
using AICalendar.Service.Contracts;
using Microsoft.Extensions.Configuration;
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

        public CalendarEventService(IHttpClientFactory httpClientFactory, IConfiguration configuration )
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        // Pass the access token from the client as a parameter
        public async Task<List<CalendarEventDto>> GetUserEventsAsync(string clientAccessToken)
        {
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

            var response = await client.GetAsync("me/events");
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
    }
}
