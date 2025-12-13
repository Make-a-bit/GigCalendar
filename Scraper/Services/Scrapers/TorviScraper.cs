using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using Scraper.Models;

namespace Scraper.Services.Scrapers
{
    /// <summary>
    /// Scraper for Torvi restaurant events.
    /// </summary>
    public class TorviScraper : IEventScraper
    {
        private readonly ICleaner _cleaner;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<TorviScraper> _logger;
        private readonly VenueRepository _venueRepository;
        private int _locationId = 0;

        public TorviScraper(ICleaner cleaner, IHttpClientFactory httpClientFactory, ILogger<TorviScraper> logger, VenueRepository venueRepository)
        {
            _cleaner = cleaner;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _venueRepository = venueRepository;
        }


        /// <summary>
        /// Gets the list of events from Torvi restaurant.
        /// </summary>
        /// <returns></returns>
        public async Task<List<Event>> ScrapeEvents()
        {
            // Initialize variables
            var events = new List<Event>();
            var doc = new HtmlDocument();
            using var client = _httpClientFactory.CreateClient();

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

                if (_locationId == 0)
                {
                    _locationId = await _venueRepository.GetVenueByNameAsync(placeNode.InnerText.Trim());
                }

                // Clean and parse details nicely for the Event object
                var eventTitle = _cleaner.EventCleaner(titleNode?.InnerText.Trim() ?? "Ei otsikkoa");
                var eventDate = ParseDate(dateNode.InnerText.ToString(), startNode.InnerText.ToString());
                var eventPrice = _cleaner.EventCleaner(priceNode?.InnerText.Trim() ?? "Ei hintatietoa");

                // Create new Event object with extracted details
                var newEvent = new Event
                {
                    Artist = eventTitle,
                    Date = eventDate,
                    PriceAsString = eventPrice,
                    Location = placeNode.InnerText.Trim() ?? "",
                    LocationId = _locationId                    
                };

                // Compare if events already contains the new scraped event.
                // If not, add it to the list.
                if (!events.Exists(e => e.Equals(newEvent)))
                {
                    events.Add(newEvent);
                    _logger.LogInformation("Added new event: {EventName} on {EventDate}", newEvent.Artist, newEvent.Date);
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
            int minutes = 0;
            var times = new string[time.Length];
            var splitChars = new char[] { '.', '-' };

            var dateStrings = date.Split(' ');
            var timeStrings = time.Split(' ');
            var dates = dateStrings[1].Split('.');
            times = timeStrings[1].Split(splitChars);

            int year = now.Year;
            int month = int.Parse(dates[1]);
            int day = int.Parse(dates[0]);
            int hours = int.Parse(times[0]);

            if (times.Length == 4)
            {
                minutes = int.Parse(times[1].Split('-')[0]);
            }

            if (month < now.Month || month == now.Month && day < now.Day)
            {
                year = now.Year + 1;
            }

            return new DateTime(year, month, day, hours, minutes, 0);
        }
    }
}