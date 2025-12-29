using Microsoft.Extensions.Logging;
using Scraper.Models;
using Scraper.Repositories;

namespace Scraper.Services.Scrapers
{
    public abstract class MediaaniScraper : BaseScraper, IEventScraper
    {
        protected readonly ICleaner _cleaner;
        protected readonly IHttpClientFactory _httpClientFactory;
        protected readonly ILogger _logger;
        protected readonly ICityRepository _cityRepository;
        protected readonly IVenueRepository _venueRepository;

        protected abstract string ScrapeUrl { get; }

        protected MediaaniScraper(
            ICleaner cleaner,
            IHttpClientFactory httpClient,
            ICityRepository cityRepository,
            IVenueRepository venueRepository,
            ILogger logger)
        {
            _cleaner = cleaner;
            _httpClientFactory = httpClient;
            _cityRepository = cityRepository;
            _venueRepository = venueRepository;
            _logger = logger;
        }

        public async Task<List<Event>> ScrapeEvents()
        {
            try
            {
                _logger.LogInformation("Starting to scrape {venue} events...", Venue.Name);

                // Initialize variables
                using var client = _httpClientFactory.CreateClient();

                // Fetch and parse the HTML document
                var html = await client.GetStringAsync(ScrapeUrl);
                Doc.LoadHtml(html);

                // Select event nodes
                var nodes = Doc.DocumentNode.SelectNodes("//div[contains(@class, 'tapahtuma-inner')]");

                _logger.LogInformation("Found {events.count} events from {venue}.", nodes.Count, Venue.Name);
                _logger.LogInformation("Starting to parse event details...");

                // Update CityId and VenueId from database
                City.Id = await _cityRepository.GetCityIdAsync(City.Name);
                Venue.Id = await _venueRepository.GetVenueIdAsync(Venue.Name, City.Id);

                // Iterate trough each event node and extract details
                foreach (var node in nodes)
                {
                    // Extract event details
                    var titlenode = node.SelectSingleNode(".//h1");
                    var datenode = node.SelectSingleNode(".//span[contains(@class, 'date-info')]");
                    var pricenode = node.SelectSingleNode(".//span[contains(@class, 'lippujen-lisatieto')]");

                    // Parse and clean event details for the Event object
                    var eventTitle = _cleaner.Clean(titlenode.InnerText);
                    var showtime = ParseShowtime(datenode.InnerText);
                    var eventPrice = _cleaner.CleanPrice(pricenode);

                    // Create new Event object with extracted details
                    var newEvent = new Event
                    {
                        EventCity = City,
                        EventVenue = Venue,
                        Artist = eventTitle,
                        Showtime = showtime,
                        HasShowtime = true,
                        Price = eventPrice,
                    };

                    _logger.LogInformation("Parsed event: {newEvent}", newEvent.ToString());

                    // Compare if events already contains the new scraped event.
                    // If not, add it to the list.
                    if (!Events.Exists(e => e.Equals(newEvent)))
                    {
                        Events.Add(newEvent);
                    }
                }

                _logger.LogInformation("Parsed {events.Count} events from {venue}.", Events.Count, Venue.Name);
                return Events;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while scraping events from {venue}.", Venue.Name);
                return Events;
            }
        }

        /// <summary>
        /// Parses the showtime string into a DateTime object.
        /// </summary>
        /// <param name="date">The showtime string containing date and time information.</param>
        /// <returns>The parsed DateTime object representing the showtime.</returns>
        private DateTime ParseShowtime(string date)
        {
            var now = DateTime.Now;
            int year, month, day, hours, minutes;

            var dateStrings = date.Split(' ');
            dateStrings = dateStrings[1].Split('.');

            year = int.Parse(dateStrings[2].Trim());
            month = int.Parse(dateStrings[1].Trim());
            day = int.Parse(dateStrings[0].Trim());

            var timeStrings = date.Split("klo ");
            timeStrings = timeStrings[1].Split(":");

            hours = int.Parse(timeStrings[0].Trim());
            minutes = int.Parse(timeStrings[1].Trim());

            return new DateTime(year, month, day, hours, minutes, 0);
        }

    }
}
