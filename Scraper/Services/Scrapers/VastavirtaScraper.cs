using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Logging;
using Scraper.Models;
using Scraper.Repositories;

namespace Scraper.Services.Scrapers
{
    public class VastavirtaScraper : BaseScraper, IEventScraper
    {
        private readonly ICleaner _cleaner;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<VastavirtaScraper> _logger;
        private readonly ICityRepository _cityRepository;
        private readonly IVenueRepository _venueRepository;

        public VastavirtaScraper(ICleaner cleaner,
        IHttpClientFactory httpClientFactory,
        ILogger<VastavirtaScraper> logger,
        ICityRepository cityRepository,
        IVenueRepository venueRepository)
        {
            _cleaner = cleaner;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _cityRepository = cityRepository;
            _venueRepository = venueRepository;

            City.Name = "Tampere";
            Venue.Name = "Vastavirta-Klubi";
        }

        public async Task<List<Event>> ScrapeEvents()
        {
            try
            {
                _logger.LogInformation("Starting to scrape Vastavirta events...");

                // Initialize variables
                using var client = _httpClientFactory.CreateClient();

                // Fetch and parse the HTML document
                var url = "https://vastavirta.net/";
                var html = await client.GetStringAsync(url);
                Doc.LoadHtml(html);

                // Select event nodes
                var nodes = Doc.DocumentNode.SelectNodes("//div[contains(@class,'vv-custom-events')]");

                _logger.LogInformation("Found {events.count} nodes from Vastavirta", nodes.Count);
                _logger.LogInformation("Starting to parse event details...");

                // Update CityID from database
                City.Id = await _cityRepository.GetCityIdAsync(City.Name);
                Venue.Id = await _venueRepository.GetVenueIdAsync(Venue.Name, City.Id);

                // Iterate through each event node and extract details
                foreach (var n in nodes)
                {
                    // Extract event details
                    var detailNodes = n.InnerText.Split("\n");
                    var eventNode = n.SelectSingleNode(".//h3");
                    var eventDetails = eventNode.InnerText.Split(" - ");
                    
                    var eventPrice = ParseEventPrice(detailNodes);
                    var showtime = ParseShowtime(eventDetails[0].Trim());

                    var newEvent = new Event
                    {
                        EventCity = City,
                        EventVenue = Venue,
                        Artist = _cleaner.Clean(eventDetails[1]),
                        Showtime = showtime,
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

                _logger.LogInformation("Parsed {events.Count} events from Vastavirta.", Events.Count);

                return Events;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while scraping events from Vastavirta");
                return Events;
            }
        }

        private static DateTime ParseShowtime(string eventDetails) 
        {
            var now = DateTime.Now;

            var showtimeDetails = eventDetails.Split(" ");
            var dateStrings = showtimeDetails[0].Split(".");
            var timeStrings = showtimeDetails[1].Split(":");

            int year = int.Parse(dateStrings[2]);
            int month = int.Parse(dateStrings[1]);
            int day = int.Parse(dateStrings[0]);

            int hours = int.Parse(timeStrings[0]);
            int minutes = int.Parse(timeStrings[1]);

            return new DateTime(year, month, day, hours, minutes, 0);
        }

        private static string ParseEventPrice(string[] eventDetails)
        {
            foreach (var detail in eventDetails)
            {
                if (detail.Contains("LOPPU"))
                {
                    return "SOLD OUT";
                }
                if (detail.Contains("Ilmainen"))
                {
                    return "Vapaa pääsy";
                }
                if (detail.Contains("Liput"))
                {
                    var priceStrings = detail.Split("Liput: ");
                    return priceStrings[1].ToString();
                }
            }

            return string.Empty;
        }
    }
}