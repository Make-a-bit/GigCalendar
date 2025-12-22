using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Scraper.Models;
using Scraper.Repositories;
using Scraper.Services.DB;
using Scraper.Services.Scrapers;
using System.Diagnostics;

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
                    Stopwatch watch = new Stopwatch();
                    watch.Start();

                    _logger.LogInformation("Starting scraping process...");

                    // Run each scraper and display results
                    foreach (var scraper in _scrapers)
                    {
                        var events = await scraper.ScrapeEvents();
                        _events = await _eventInspector.UpdateRepositoriesAsync(events, _events);
                    }
                    
                    // Remove old events from the database and list
                    await _remover.CleanupOldEvents(_events);
                    _events.RemoveAll(e => e.Showtime.Date < DateTime.Now.Date);

                    watch.Stop();

                    // CalculateSeconds delay until next run
                    var delay = Delay.CalculateNextRun();
                    _logger.LogInformation("Scraping completed and took {hours}:{minutes}:{seconds}. " +
                        "Current events count {events}. Next run at {NextRun}",
                        watch.Elapsed.Hours,
                        watch.Elapsed.Minutes,
                        watch.Elapsed.Seconds,
                        _events.Count,
                        DateTime.Now.Add(delay).ToString("yyyy-MM-dd HH:mm"));

                    await Task.Delay(delay, stoppingToken);
                }
                catch (OperationCanceledException) 
                {
                    _logger.LogInformation("Scraper service is stopping due to cancellation request...");
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
    }
}
