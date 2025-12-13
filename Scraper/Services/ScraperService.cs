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
        private readonly IServiceProvider _serviceProvider;
        private readonly EventInspector _eventInspector;
        private readonly EventRepository _eventRepository;
        private readonly ResultsWriter _results;

        private List<Event> _events;

        public ScraperService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _eventInspector = serviceProvider.GetRequiredService<EventInspector>();
            _eventRepository = serviceProvider.GetRequiredService<EventRepository>();
            _logger = serviceProvider.GetRequiredService<ILogger<ScraperService>>();
            _results = serviceProvider.GetRequiredService<ResultsWriter>();

            _events = new();
        }


        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Scraper Service is starting...");
            _events = await _eventRepository.GetEventsAsync();

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Scrapers to run
                    var scrapers = new List<IEventScraper>
                    {
                         _serviceProvider.GetRequiredService<TorviScraper>(),
                         //_serviceProvider.GetRequiredService<SibeliustaloScraper>(),
                         //_serviceProvider.GetRequiredService<MusaklubiScraper>(),
                         //_serviceProvider.GetRequiredService<TavastiaScraper>(),
                         //_serviceProvider.GetRequiredService<SemifinalScraper>(),
                        // Kapakanmäki
                        // G Livelab Helsinki
                        // Kulttuuritalo
                        // Rytmikorjaamo
                        // Sammiosalo
                        // Vanha valimo (Lahti)
                    };

                    _logger.LogInformation("========== Scrapers collected: ========== \n\t{Scrapers}",
                        string.Join(",\n\t", scrapers.Select(s => s.GetType().Name)));

                    // Run each scraper and display results
                    foreach (var scraper in scrapers)
                    {
                        var events = await scraper.ScrapeEvents();

                        // TODO: Compare found events with existing events in list and DB and add new ones.
                        _events = await _eventInspector.UpdateEventsDataAsync(events, _events);

                        _results.WriteResultsToConsole(events);
                    }
                }
                catch (Exception ex)
                {

                }
            }

            _logger.LogInformation("Closing down scraper service...");
        }
    }
}
