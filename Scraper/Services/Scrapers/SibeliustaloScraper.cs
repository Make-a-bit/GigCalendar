using HtmlAgilityPack;
using Scraper.Models;

namespace Scraper.Services.Scrapers
{
    public class SibeliustaloScraper : IEventScraper
    {
        private readonly ICleaner _cleaner;
        private readonly IHttpClientFactory _httpClientFactory;

        public int LocationId;
        public string LocationName;

        public SibeliustaloScraper(ICleaner cleaner, IHttpClientFactory httpClientFactory)
        {
            _cleaner = cleaner;
            _httpClientFactory = httpClientFactory; 
        }



        public async Task<List<Event>> ScrapeEvents()
        {
            // Initialize variables
            var events = new List<Event>();
            var doc = new HtmlDocument();
            using var client = _httpClientFactory.CreateClient();

            // Fetch and parse the HTML document
            var url = "https://www.sibeliustalo.fi/tapahtumakalenteri/";
            var html = await client.GetStringAsync(url);
            doc.LoadHtml(html);

            // Select event nodes
            var nodes = doc.DocumentNode.SelectNodes("//div[contains(@class,'col-md-9')]");

            // Iterate through each event node and extract details
            foreach (var n in nodes)
            {
                // Extract event details
                var titleNode = n.SelectSingleNode(".//h2");
                var dateNode = n.SelectSingleNode(".//div[contains(@class, 'field-type-datetime')]");
                var priceNode = n.SelectSingleNode(".//div[contains(@class, 'ticket-info')]");
                var placeNode = n.SelectSingleNode(".//h6");

                // Clean and parse details nicely for the Event object
                var eventTitle = _cleaner.EventCleaner(titleNode?.InnerText.Trim() ?? "Ei otsikkoa");
                var eventDate = ParseDate(dateNode.InnerText.ToString().Trim());
                var eventPrice = _cleaner.PriceCleaner(priceNode?.InnerText.Trim() ?? "Ei hintatietoa");

                // Create new Event object with extracted details
                var newEvent = new Event
                {
                    Artist = eventTitle,
                    Date = eventDate,
                    PriceAsString = eventPrice,
                    Location = placeNode.InnerText.Trim() ?? ""
                };

                // Compare if events already contains the new event. If not, add it to the list.
                if (!events.Exists(e => e.Equals(newEvent)))
                {
                    events.Add(newEvent);
                }
            }
            // Return the list of events
            return events;

        }

        /// <summary>
        /// Parses a date string in the format dd.MM and returns a DateTime object.
        /// </summary>
        /// <param name="date">The date string to parse.</param>
        /// <returns>A DateTime object representing the parsed date.</returns>
        private static DateTime ParseDate(string date)
        {
            var now = DateTime.Now;

            var strings = date.Split(' ');
            var dates = strings[1].Split('.');

            int year = int.Parse(dates[2]);
            int month = int.Parse(dates[1]);
            int day = int.Parse(dates[0]);
            int hours = int.Parse(strings[3].Split('.')[0]);
            int minutes = int.Parse(strings[3].Split('.')[1]);

            if (month < now.Month || month == now.Month && day < now.Day)
            {
                year = now.Year + 1;
            }

            return new DateTime(year, month, day, hours, minutes, 0);
        }
    }
}
