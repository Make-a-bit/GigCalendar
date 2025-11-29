using HtmlAgilityPack;
using Scraper.Models;

namespace Scraper.Services
{
    /// <summary>
    /// Scraper for Torvi restaurant events.
    /// </summary>
    public class TorviScraper : IEventScraper
    {
        private ICleaner _cleaner;
        public string Source => "Torvi";

        public TorviScraper(ICleaner cleaner)
        {
            _cleaner = cleaner;
        }


        /// <summary>
        /// Gets the list of events from Torvi restaurant.
        /// </summary>
        /// <returns></returns>
        public async Task<List<Event>> GetEvents()
        {
            // Initialize variables
            var events = new List<Event>();
            var doc = new HtmlDocument();
            using var client = new HttpClient();

            // Fetch and parse the HTML document
            var url = "https://ravintolatorvi.fi/tapahtumat/";
            var html = await client.GetStringAsync(url);
            doc.LoadHtml(html);

            // Select event nodes
            var nodes = doc.DocumentNode.SelectNodes("//div[contains(@class,'event')]");

            // Iterate through each event node and extract details
            foreach (var n in nodes)
            {
                // Extract event details
                var titleNode = n.SelectSingleNode(".//h3");
                var dateNode = n.SelectSingleNode(".//p[contains(@class, 'date')]");
                var startNode = n.SelectSingleNode(".//li[contains(@class, 'start')]");
                var priceNode = n.SelectSingleNode(".//li[contains(@class, 'price')]");
                var placeNode = n.SelectSingleNode(".//li[contains(@class, 'place')]");

                // Clean and parse details nicely for the Event object
                var eventTitle = _cleaner.Cleaner(titleNode?.InnerText.Trim() ?? "Ei otsikkoa");
                var eventDate = ParseDate(dateNode.InnerText.ToString(), startNode.InnerText.ToString());
                var eventPrice = _cleaner.Cleaner(priceNode?.InnerText.Trim() ?? "Ei hintatietoa");

                // Create new Event object with extracted details
                var newEvent = new Event
                {
                    Name = eventTitle,
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
        private static DateTime ParseDate(string date, string time)
        {
            var now = DateTime.Now;

            var dateStrings = date.Split(' ');
            var timeStrings = time.Split(' ');
            var dates = dateStrings[1].Split('.');
            var times = timeStrings[1].Split('.');

            int year = now.Year;
            int month = int.Parse(dates[1]);
            int day = int.Parse(dates[0]);
            int hours = int.Parse(times[0]);
            int minutes = int.Parse(times[1].Split('-')[0]);

            if (month < now.Month || month == now.Month && day < now.Day)
            {
                year = now.Year + 1;
            }

            return new DateTime(year, month, day, hours, minutes, 0);
        }

    }
}
