using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using Scraper.Models;
using Scraper.Repositories;

namespace Scraper.Services.Scrapers
{
    /// <summary>
    /// Scraper for Torvi restaurant events.
    /// </summary>
    public class TorviScraper : BaseScraper, IEventScraper
    {
        private readonly ICleaner _cleaner;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<TorviScraper> _logger;
        private readonly ICityRepository _cityRepository;
        private readonly IVenueRepository _venueRepository;

        public TorviScraper(ICleaner cleaner, 
            IHttpClientFactory httpClientFactory, 
            ILogger<TorviScraper> logger, 
            ICityRepository cityRepository,
            IVenueRepository venueRepository)
        {
            _cleaner = cleaner;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _cityRepository = cityRepository;
            _venueRepository = venueRepository;

            City.Name = "Lahti";
        }


        /// <summary>
        /// Gets the list of events from Torvi.
        /// </summary>
        /// <returns></returns>
        public async Task<List<Event>> ScrapeEvents()
        {
            try
            {
                _logger.LogInformation("Starting to scrape Torvi events...");

                // Initialize variables
                using var client = _httpClientFactory.CreateClient();

                // Fetch and parse the HTML document
                var url = "https://ravintolatorvi.fi/tapahtumat/";
                var html = await client.GetStringAsync(url);
                Doc.LoadHtml(html);

                // Select event nodes
                var nodes = Doc.DocumentNode.SelectNodes("//div[contains(@class,'event')]");

                _logger.LogInformation("Found {events.count} nodes from Torvi", nodes.Count);
                _logger.LogInformation("Starting to parse event details...");

                // Update CityID from database
                City.Id = await _cityRepository.GetCityIdAsync(City.Name);

                // Iterate through each event node and extract details
                foreach (var n in nodes)
                {
                    // Extract event details
                    var titleNode = n.SelectSingleNode(".//h3");
                    var dateNode = n.SelectSingleNode(".//p[contains(@class, 'date')]");
                    var startNode = n.SelectSingleNode(".//li[contains(@class, 'start')]");
                    var priceNode = n.SelectSingleNode(".//li[contains(@class, 'price')]");
                    var placeNode = n.SelectSingleNode(".//li[contains(@class, 'place')]");

                    // Update VenueID from database
                    Venue.Id = await _venueRepository.GetVenueIdAsync(placeNode.InnerText.Trim(), City.Id);
                    Venue.Name = placeNode.InnerText.Trim();

                    // Clean and parse event details for the Event object
                    var eventTitle = _cleaner.Clean(titleNode.InnerText.Trim());
                    var eventDate = ParseDate(dateNode.InnerText.ToString(), startNode.InnerText.ToString());
                    var eventPrice = _cleaner.CleanPrice(priceNode);

                    // Create new Event object with extracted details
                    var newEvent = new Event
                    {
                        EventCity = City,
                        EventVenue = Venue,
                        Artist = eventTitle,
                        Showtime = eventDate,
                        HasShowtime = true,
                        Price = eventPrice
                    };

                    _logger.LogInformation("Parsed event: {newEvent}", newEvent.ToString());

                    // Compare if events already contains the new scraped event.
                    // If not, add it to the list.
                    if (!Events.Exists(e => e.Equals(newEvent)))
                    {
                        Events.Add(newEvent);
                    }
                }

                _logger.LogInformation("Parsed {events.Count} events from Torvi.", Events.Count);

                // Return the list of events
                return Events;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while scraping events from Torvi.");
                return Events;
            }
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

            // Handle cases where minutes might not be provided
            if (times.Length == 4)
            {
                minutes = int.Parse(times[1].Split('-')[0]);
            }

            // Adjust year if the date has already passed this year
            if (month < now.Month || month == now.Month && day < now.Day)
            {
                year = now.Year + 1;
            }

            return new DateTime(year, month, day, hours, minutes, 0);
        }
    }
}