using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using Scraper.Models;
using Scraper.Repositories;

namespace Scraper.Services.Scrapers
{
    public class SibeliustaloScraper : BaseScraper, IEventScraper
    {
        private readonly ICleaner _cleaner;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<SibeliustaloScraper> _logger;
        private readonly ICityRepository _cityRepository;
        private readonly IVenueRepository _venueRepository;

        public SibeliustaloScraper(ICleaner cleaner, 
            IHttpClientFactory httpClientFactory, 
            ILogger<SibeliustaloScraper> logger, 
            ICityRepository cityRepository,
            IVenueRepository venueRepository)
        {
            _cleaner = cleaner;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _cityRepository = cityRepository;
            _venueRepository = venueRepository;

            City.Name = "Lahti";
            Venue.Name = "Sibeliustalo";
        }


        /// <summary>
        /// Gets the list of events from Sibeliustalo.
        /// </summary>
        /// <returns></returns>
        public async Task<List<Event>> ScrapeEvents()
        {
            try
            {
                _logger.LogInformation("Starting to scrape Sibeliustalo events...");

                // Initialize variables
                using var client = _httpClientFactory.CreateClient();

                // Fetch and parse the HTML document
                var url = "https://www.sibeliustalo.fi/tapahtumakalenteri/";
                var html = await client.GetStringAsync(url);
                Doc.LoadHtml(html);

                // Select event nodes
                var nodes = Doc.DocumentNode.SelectNodes("//div[contains(@class,'col-md-9')]");

                _logger.LogInformation("Found {events.count} nodes from Sibeliustalo.", nodes.Count);
                _logger.LogInformation("Starting to parse event details...");

                // Update CityId and VenueId from database
                City.Id = await _cityRepository.GetCityIdAsync(City.Name);
                Venue.Id = await _venueRepository.GetVenueIdAsync(Venue.Name, City.Id);

                // Iterate through each event node and extract details
                foreach (var n in nodes)
                {
                    // Extract event details
                    var titleNode = n.SelectSingleNode(".//h2");
                    var dateNode = n.SelectSingleNode(".//div[contains(@class, 'field-type-datetime')]");
                    var priceNode = n.SelectSingleNode(".//div[contains(@class, 'ticket-info')]");
                    var priceText = _cleaner.ReplacePrefixes(priceNode.InnerText);
                    var prices = priceText.Split("/");               
                    var placeNode = n.SelectSingleNode(".//h6");

                    // Clean and parse event details for the Event object
                    var eventTitle = _cleaner.Clean(titleNode.InnerText.Trim());
                    var showtime = ParseShowtime(dateNode.InnerText.ToString().Trim());
                    var eventPrice = _cleaner.CleanPrice(prices);

                    // Create new Event object with extracted details
                    var newEvent = new Event
                    {
                        EventCity = City,
                        EventVenue = Venue,
                        Artist = eventTitle,
                        Showtime = showtime,
                        HasShowtime = true,
                        Price = eventPrice
                    };

                    _logger.LogInformation("Parsed event: {newEvent}", newEvent.ToString());

                    // Compare if events already contains the new event. If not, add it to the list.
                    if (!Events.Exists(e => e.Equals(newEvent)))
                    {
                        Events.Add(newEvent);
                    }
                }

                _logger.LogInformation("Parsed {events.Count} events from Sibeliustalo.", Events.Count);

                // Return the list of events
                return Events;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while parsing Sibeliustalo events.");
                return Events;
            }
        }

        /// <summary>
        /// Parses a date string in the format dd.MM and returns a DateTime object.
        /// </summary>
        /// <param name="date">The date string to parse.</param>
        /// <returns>A DateTime object representing the parsed date.</returns>
        private static DateTime ParseShowtime(string date)
        {
            var now = DateTime.Now;

            var strings = date.Split(' ');
            var dates = strings[1].Split('.');

            int year = int.Parse(dates[2]);
            int month = int.Parse(dates[1]);
            int day = int.Parse(dates[0]);
            int hours = int.Parse(strings[3].Split('.')[0]);
            int minutes = int.Parse(strings[3].Split('.')[1]);

            // Adjust year if the date has already passed this year
            if (month < now.Month || month == now.Month && day < now.Day)
            {
                year = now.Year + 1;
            }

            return new DateTime(year, month, day, hours, minutes, 0);
        }
    }
}
