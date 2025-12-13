using Scraper.Models;

namespace Scraper
{
    public class ResultsWriter
    {
        public void WriteResultsToConsole(List<Event> events)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            foreach (var ev in events)
            {
                Console.WriteLine($"Artist: {ev.Artist}");
                Console.WriteLine($"Date: {ev.Date}");
                Console.WriteLine($"Location: {ev.Location}");
                Console.WriteLine($"Price: {ev.PriceAsString}");
                Console.WriteLine(new string('-', 40));
            }
        }
    }
}