using Microsoft.Extensions.Logging;
using Scraper.Models;
using Scraper.Services.DB;

namespace Scraper.Services
{
    public interface IEventInspector
    {
        /// <summary>
        /// Updates the event repositories with new events. 
        /// </summary>
        /// <param name="scrapedEvents">Scraped events updated from the source</param>
        /// <param name="currentEvents">Current events fetched from database</param>
        /// <remarks>If an event from scrapedEvents is not found in currentEvents, 
        /// it is added to the current list as well as into the database.</remarks>
        /// <returns></returns>
        public Task<List<Event>> UpdateRepositoriesAsync(List<Event> scrapedEvents, List<Event> currentEvents);
    }

    public class EventInspector : IEventInspector
    {
        private readonly ILogger<EventInspector> _logger;
        private readonly IEventAdder _adder;

        public EventInspector(IEventAdder adder, ILogger<EventInspector> logger)
        {
            _adder = adder;
            _logger = logger;
        }

        public async Task<List<Event>> UpdateRepositoriesAsync(List<Event> scrapedEvents, List<Event> currentEvents)
        {
            // Check each scraped event against current events list
            foreach (var item in scrapedEvents)
            {
                // If scraped event already exists on the list, skip to next
                if (currentEvents.Contains(item))
                {
                    continue;
                }

                // Add new event to database and current events list
                var result = await _adder.AddIntoDatabase(item);

                if (result)
                {
                    currentEvents.Add(item);
                    _logger.LogInformation("Added new event to database: {Event}", item);
                }
                else
                {
                    _logger.LogWarning("Failed to add event to database: {Event}", item);
                }
            }
            return currentEvents;
        }
    }
}
