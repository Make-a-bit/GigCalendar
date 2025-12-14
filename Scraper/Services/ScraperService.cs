using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Scraper.Models;
using Scraper.Services.DB;
using Scraper.Services.Scrapers;

namespace Scraper.Services
{
    public class ScraperService : BackgroundService
    {
        private readonly ILogger _logger;
        private readonly IEnumerable<IEventScraper> _scrapers;

        private readonly IEventInspector _eventInspector;
        private readonly IEventRepository _eventRepository;

        private List<Event> _events;

        public ScraperService(IEnumerable<IEventScraper> scrapers,
            IEventInspector eventInspector,
            IEventRepository eventRepository,
            ILogger<ScraperService> logger)
        {
            _scrapers = scrapers;
            _eventInspector = eventInspector;
            _eventRepository = eventRepository;
            _logger = logger;

            _events = new();
        }


        /// <summary>
        /// Main execution loop for the scraper service.
        /// </summary>
        /// <param name="stoppingToken"></param>
        /// <returns></returns>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Scraper Service is starting...");
            _events = await _eventRepository.GetEventsAsync();

            // Run main loop until cancellation is requested
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("Starting scraping process...");

                    // Run each scraper and display results
                    foreach (var scraper in _scrapers)
                    {
                        var events = await scraper.ScrapeEvents();
                        _events = await _eventInspector.UpdateEventsRepositoriesAsync(events, _events);
                    }

                    // Calculate delay until next run
                    var delay = CalculateDelay();
                    _logger.LogInformation("Scraping process completed. Next run in {Delay} hours.", delay.TotalHours);

                    await Task.Delay(delay, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while scraping events");
                }
            }

            _logger.LogInformation("Closing down scraper service...");
        }


        /// <summary>
        /// Calculates the delay until the next scheduled run at 09:00, 3 days from now.
        /// </summary>
        /// <returns></returns>
        private static TimeSpan CalculateDelay()
        {
            // Run at 09:00, 3 days from now
            var now = DateTime.Now;
            var nextRun = DateTime.Today.AddDays(3).AddHours(9);

            // If we've already passed 09:00 today, this ensures we don't go backwards
            if (nextRun <= now)
            {
                nextRun = nextRun.AddDays(1);
            }

            return nextRun - now;
        }
    }
}
