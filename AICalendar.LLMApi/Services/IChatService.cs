using AICalendar.LLMApi.Models;

namespace AICalendar.LLMApi.Services
{
    public interface IChatService
    {
        Task<string> ProcessMessageAsync(string message);
    }
}
