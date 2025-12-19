using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Scraper.Repositories;
using Scraper.Services;
using Scraper.Services.DB;
using Scraper.Services.Scrapers;

namespace Scraper
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            CreateHostBuilder(args).Build().Run();
        }

        private static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((context, services) =>
                {
                    services.AddHttpClient();
                    services.AddLogging(configure =>
                    {
                        configure.AddConsole();
                        configure.AddFilter("Scraper.Services", LogLevel.Information);
                    });

                    services.AddHostedService<ScraperService>();
                    services.AddSingleton<ICleaner, StringCleaner>();
                    services.AddSingleton<DBManager>();
                    services.AddSingleton<ICityRepository, CityRepository>();
                    services.AddSingleton<IEventRepository, EventRepository>();
                    services.AddSingleton<IVenueRepository, VenueRepository>();
                    services.AddSingleton<IEventUpdate, EventUpdate>();
                    services.AddTransient<IEventAdder, EventAdder>();
                    services.AddTransient<IEventRemover, EventRemover>();
                    services.AddTransient<IEventInspector, EventInspector>();
                    services.AddTransient<IEventScraper, MusaklubiScraper>();
                    services.AddTransient<IEventScraper, SemifinalScraper>();
                    services.AddTransient<IEventScraper, SibeliustaloScraper>();
                    services.AddTransient<IEventScraper, TavastiaScraper>();
                    services.AddTransient<IEventScraper, TorviScraper>();
                });

    }
}