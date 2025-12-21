using Scraper.Models;

namespace Scraper.Services
{
    public interface IEventScraper
    {
        /// <summary>
        /// Scrapes events from the source website.
        /// </summary>
        /// <returns>A list of events scraped from the source.</returns>
        public Task<List<Event>> ScrapeEvents();
    }
}
