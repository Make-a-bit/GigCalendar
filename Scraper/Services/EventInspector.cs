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
        /// <remarks>If an event from scrapedEvents is not found in currentEvents, 
        /// it is added to the current list as well as into the database.</remarks>
        /// <param name="scrapedEvents">Scraped events updated from the source</param>
        /// <param name="currentEvents">Current events fetched from database</param>
        /// <returns>Updated list of current events</returns>
        public Task<List<Event>> UpdateRepositoriesAsync(List<Event> scrapedEvents, List<Event> currentEvents);
    }

    public class EventInspector : IEventInspector
    {
        private readonly ILogger<EventInspector> _logger;
        private readonly IEventAdder _adder;
        private readonly IEventUpdate _updater;

        public EventInspector(IEventAdder adder,  ILogger<EventInspector> logger, IEventUpdate updater)
        {
            _adder = adder;
            _logger = logger;
            _updater = updater;
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

                // Find if item already exists (ignore price)
                var existingEvent = currentEvents.Find(e => e.IsSameEvent(item));

                // If event exists but price has changed, update the price
                if (existingEvent != null && (existingEvent.PriceAsString != item.PriceAsString))
                {
                    existingEvent.PriceAsString = item.PriceAsString;
                    var result = await _updater.UpdatePriceAsync(existingEvent);

                    // If update was successful, update current events list and log it
                    if (result)
                    {
                        currentEvents.Remove(existingEvent);
                        currentEvents.Add(item);
                        _logger.LogInformation("Updated existing event to database: {Event}", item);
                    }
                    else
                    {
                        _logger.LogWarning("Failed to update event to database: {Event}", item);
                    }

                    continue;
                }

                // If event does not exist, add it to database
                if (existingEvent == null)
                {
                    var result = await _adder.AddIntoDatabase(item);

                    // If addition was successful, add to current events list and log it
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
            }
            return currentEvents;
        }
    }
}
