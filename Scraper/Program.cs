using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Scraper.Services;
using Scraper.Services.DB;
using Scraper.Services.Scrapers;

namespace Scraper
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
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

                    services.AddSingleton<ICleaner, StringCleaner>();
                    services.AddSingleton<DBManager>();
                    services.AddTransient<EventAdder>();
                    services.AddTransient<EventInspector>();
                    services.AddSingleton<EventRepository>();
                    services.AddTransient<MusaklubiScraper>();
                    services.AddSingleton<ResultsWriter>();
                    services.AddHostedService<ScraperService>();
                    services.AddTransient<SemifinalScraper>();
                    services.AddTransient<SibeliustaloScraper>();
                    services.AddTransient<TavastiaScraper>();
                    services.AddTransient<TorviScraper>();
                    services.AddTransient<VenueRepository>();

                    var serviceProvider = services.BuildServiceProvider();
                });

    }
}