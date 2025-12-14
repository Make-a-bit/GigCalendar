using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using Scraper.Models;

namespace Scraper.Services.Scrapers
{
    public class MusaklubiScraper : IEventScraper
    {
        private ICleaner _cleaner;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<MusaklubiScraper> _logger;
        private readonly IVenueRepository _venueRepository;

        public string _locationName = "Möysän Musaklubi";
        public int _locationId = -1;

        public MusaklubiScraper(ICleaner cleaner, 
            IHttpClientFactory httpClientFactory, 
            IVenueRepository venueRepository, 
            ILogger<MusaklubiScraper> logger)
        {
            _cleaner = cleaner;
            _httpClientFactory = httpClientFactory;
            _venueRepository = venueRepository;
            _logger = logger;
        }



        /// <summary>
        /// Gets the list of events from Musaklubi
        /// </summary>
        /// <returns></returns>
        public async Task<List<Event>> ScrapeEvents()
        {
            try
            {
                // Initialize variables
                var events = new List<Event>();
                HtmlDocument doc = new HtmlDocument();
                HtmlDocument innerDoc = new HtmlDocument();
                using var client = _httpClientFactory.CreateClient();

                // Fetch and parse the HTML document
                var url = "https://moysa.fi/keikat/";
                var html = await client.GetStringAsync(url);
                doc.LoadHtml(html);

                // Select event nodes
                var htmlNodes = doc.DocumentNode.SelectSingleNode("//div[contains(@class,'mec-event-list-classic')]");
                innerDoc.LoadHtml(htmlNodes.InnerHtml);
                var nodes = innerDoc.DocumentNode.SelectNodes(".//article[contains(@class,'mec-event-article')]");

                // Check location ID
                _locationId = await _venueRepository.GetVenueIdAsync(_locationName);

                // Iterate through each event node and extract details
                foreach (var n in nodes)
                {
                    // Extract event details
                    var titleNode = n.SelectSingleNode(".//h4");
                    var dateNode = n.SelectSingleNode(".//span[contains(@class, 'mec-start-date')]");

                    // Clean and parse details nicely for the Event object
                    var eventTitle = _cleaner.EventCleaner(titleNode?.InnerText.Trim() ?? "Ei otsikkoa");
                    var eventDate = ParseDate(dateNode.InnerText.ToString());

                    // Create new Event object with extracted details
                    var newEvent = new Event
                    {
                        Artist = eventTitle,
                        Date = eventDate,
                        PriceAsString = "Ei hintatietoa",
                        Location = _locationName,
                        LocationId = _locationId
                    };

                    // Compare if events already contains the new event. If not, add it to the list.
                    if (!events.Exists(e => e.Equals(newEvent)))
                    {
                        events.Add(newEvent);
                    }
                }

                _logger.LogInformation("Scraped {events.Count} events from Musaklubi.", events.Count);

                // Return the list of events
                return events;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while scraping events from Musaklubi.");
                return new List<Event>();
            }
        }


        /// <summary>
        /// Parses a date string in the format dd.MM and returns a DateTime object.
        /// </summary>
        /// <param name="date">The date string to parse.</param>
        /// <returns>A DateTime object representing the parsed date.</returns>
        private static DateTime ParseDate(string date)
        {
            var now = DateTime.Now;

            var dateStrings = date.Split(' ');

            int year = int.Parse(dateStrings[2]);
            int month = MonthConvert(dateStrings[0]);
            int day = int.Parse(dateStrings[1]);

            if (month < now.Month || month == now.Month && day < now.Day)
            {
                year = now.Year + 1;
            }

            return new DateTime(year, month, day);
        }


        /// <summary>
        /// Converts Finnish month names to their corresponding month numbers.
        /// </summary>
        /// <param name="month">The Finnish month name to convert.</param>
        /// <returns>The corresponding month number, or 0 if not found.</returns>
        private static int MonthConvert(string month)
        {
            switch (month.ToLower())
            {
                case "tammi":
                    return 1;
                case "helmi":
                    return 2;
                case "maalis":
                    return 3;
                case "huhti":
                    return 4;
                case "touko":
                    return 5;
                case "kesä":
                    return 6;
                case "heinä":
                    return 7;
                case "elo":
                    return 8;
                case "syys":
                    return 9;
                case "loka":
                    return 10;
                case "marras":
                    return 11;
                case "joulu":
                    return 12;
                default:
                    return 0;
            }
        }
    }
}
