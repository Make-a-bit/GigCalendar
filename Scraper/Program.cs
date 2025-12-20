using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Scraper.Repositories;
using Scraper.Services;
using Scraper.Services.DB;

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
                    services.AddSingleton<ICleaner, Cleaner>();
                    services.AddSingleton<DBManager>();
                    services.AddSingleton<ICityRepository, CityRepository>();
                    services.AddSingleton<IEventRepository, EventRepository>();
                    services.AddSingleton<IVenueRepository, VenueRepository>();
                    services.AddTransient<IEventAdder, EventAdder>();
                    services.AddTransient<IEventInspector, EventInspector>();
                    services.AddTransient<IEventRemover, EventRemover>();
                    services.AddTransient<IEventUpdate, EventUpdate>();

                    // Automated registration for all scrapers
                    var scraperTypes = typeof(Program).Assembly.GetTypes()
                    .Where(t => typeof(IEventScraper).IsAssignableFrom(t)
                    && !t.IsInterface
                    && !t.IsAbstract);

                    foreach (var scraperType in scraperTypes)
                    {
                        services.AddTransient(typeof(IEventScraper), scraperType);
                    }
                });

    }
}