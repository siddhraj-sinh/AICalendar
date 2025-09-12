using AICalendar.Client.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web;

namespace AICalendar.Client.Pages;

public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    //private readonly IDownstreamApi _downstreamApi;

    public List<CalendarEventDto> Events { get; set; } = new();

    public IndexModel(ILogger<IndexModel> logger)
    {
        _logger = logger;
        //_downstreamApi = downstreamApi;
    }

    public async Task OnGetAsync()
    {
        //Events = await _downstreamApi.CallApiForUserAsync<List<CalendarEventDto>>("CalendarApi", options =>
        //{
        //    options.RelativePath = "/api/CalendarEvent";
        //});
    }
}
