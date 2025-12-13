using Microsoft.Extensions.Logging;
using Scraper.Models;
using Scraper.Services.DB;

namespace Scraper.Services
{
    public class EventInspector
    {
        private readonly ILogger<EventInspector> _logger;
        private readonly EventAdder _adder;

        public EventInspector(EventAdder adder, ILogger<EventInspector> logger)
        {
            _adder = adder;
            _logger = logger;
        }


        public async Task<List<Event>> UpdateEventsDataAsync(List<Event> updatedEventsList, List<Event> currentEventsList)
        {
            var tempList = currentEventsList;
            var result = false;

            foreach (var item in updatedEventsList)
            {
                if (currentEventsList.Contains(item))
                {
                    continue;
                }

                result = await _adder.AddIntoDatabase(item);

                if (result)
                {
                    tempList.Add(item);
                    _logger.LogInformation("Added new event to database: {Event}", item);
                }
                else
                {
                    _logger.LogWarning("Failed to add event to database: {Event}", item);
                }
            }
            return tempList;
        }
    }
}
