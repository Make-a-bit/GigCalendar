using Scraper.Models;

namespace Scraper.Services
{
    public interface IEventScraper
    {
        public Task<List<Event>> ScrapeEvents();
    }
}
