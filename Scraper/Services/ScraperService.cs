using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Scraper.Models;
using Scraper.Repositories;
using Scraper.Services.DB;

namespace Scraper.Services
{
    public class ScraperService : BackgroundService
    {
        private readonly ILogger _logger;
        private readonly IEnumerable<IEventScraper> _scrapers;

        private readonly IEventInspector _eventInspector;
        private readonly IEventRepository _eventRepository;
        private readonly IEventRemover _remover;

        private List<Event> _events;

        public ScraperService(IEnumerable<IEventScraper> scrapers,
            IEventInspector eventInspector,
            IEventRepository eventRepository,
            ILogger<ScraperService> logger,
            IEventRemover remover)
        {
            _scrapers = scrapers;
            _eventInspector = eventInspector;
            _eventRepository = eventRepository;
            _logger = logger;

            _events = new();
            _remover = remover;
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
                        _events = await _eventInspector.UpdateRepositoriesAsync(events, _events);
                    }
                    
                    // Remove old events from the database and list
                    await _remover.CleanupOldEvents(_events);
                    _events.RemoveAll(e => e.Date.Date < DateTime.Now.Date);

                    // Calculate delay until next run
                    var delay = CalculateDelay();
                    _logger.LogInformation("Scraping completed. Next run at {NextRun}",
                        DateTime.Now.Add(delay).ToString("yyyy-MM-dd HH:mm"));

                    await Task.Delay(delay, stoppingToken);
                }
                catch (OperationCanceledException) 
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while scraping events");

                    // Wait 1 hour before retrying on error to avoid tight error loop
                    var errorDelay = TimeSpan.FromHours(1);
                    _logger.LogInformation("Retrying in {Hours} hour(s)...", errorDelay.TotalHours);

                    await Task.Delay(errorDelay, stoppingToken);
                }
            }

            _logger.LogInformation("Closing down scraper service...");
        }


        /// <summary>
        /// Calculates the delay until the next scheduled run at 03:00
        /// </summary>
        /// <returns></returns>
        private static TimeSpan CalculateDelay()
        {
            var now = DateTime.Now;
            var nextRun = DateTime.Today.AddDays(1).AddHours(3);

            var delay = nextRun - now;

            // Safety check: if delay is negative or too small, schedule for next day
            if (delay.TotalHours < 1)
            {
                nextRun = nextRun.AddDays(1);
                delay = nextRun - now;
            }

            return delay;
        }
    }
}
