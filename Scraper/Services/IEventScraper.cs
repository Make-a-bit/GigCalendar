using Scraper.Models;

namespace Scraper.Services
{
    public interface IEventScraper
    {
        public string Source { get; }
        public Task<List<Event>> GetEvents();
    }
}
