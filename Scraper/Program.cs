using Scraper.Services;

namespace Scraper
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            var cleaner = new StringCleaner();

            // Scrapers to run
            var scrapers = new List<IEventScraper>
            {
                new TorviScraper(cleaner),
                new SibeliustaloScraper(cleaner),
            };

            // Run each scraper and display results
            foreach (var scraper in scrapers)
            {
                var events = await scraper.GetEvents();
                Console.WriteLine($"========== {scraper.Source} ==========");
                
                foreach (var ev in events)
                {
                    Console.WriteLine($"- {ev.Name}: {ev.Date.ToString()}, {ev.PriceAsString}");
                }
                Console.WriteLine("\n");
            }
        }
    }
}
