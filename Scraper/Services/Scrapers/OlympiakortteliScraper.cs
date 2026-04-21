using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Logging;
using Scraper.Models;
using Scraper.Repositories;

namespace Scraper.Services.Scrapers
{
    public class OlympiakortteliScraper : BaseScraper, IEventScraper
    {
        private readonly ICleaner _cleaner;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<VastavirtaScraper> _logger;
        private readonly ICityRepository _cityRepository;
        private readonly IVenueRepository _venueRepository;

        public OlympiakortteliScraper(ICleaner cleaner,
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
            Venue.Name = "Olympia-kortteli";
        }

        public async Task<List<Event>> ScrapeEvents()
        {
            try
            {
                _logger.LogInformation("Starting to scrape Olympiakortteli events...");

                // Initialize variables
                using var client = _httpClientFactory.CreateClient();

                // Fetch and parse the HTML document
                var url = "https://olympiakortteli.fi/keikat/";
                var html = await client.GetStringAsync(url);
                Doc.LoadHtml(html);

                // Select event nodes
                var nodes = Doc.DocumentNode.SelectNodes("//div[contains(@class,'grid-x keikka')]");

                _logger.LogInformation("Found {events.count} nodes from Olympiakortteli", nodes.Count);
                _logger.LogInformation("Starting to parse event details...");

                // Update CityID and VenueID from database
                City.Id = await _cityRepository.GetCityIdAsync(City.Name);
                Venue.Id = await _venueRepository.GetVenueIdAsync(Venue.Name, City.Id);

                // Iterate through each event node and extract details
                foreach (var n in nodes)
                {
                    try
                    {
                        var detailsNode = n.SelectSingleNode(".//div[contains(@class, 'cell large-7 small-12 text-center large-text-left infot align-self-middle')]");
                        var eventDateNode = n.SelectSingleNode(".//div[contains(@class, 'cell hide-for-large small-12 pvm shrink text-center align-self-middle')]");
                        var notesNode = n.SelectSingleNode(".//div[contains(@class, 'lisatiedot')]");

                        if (detailsNode == null || eventDateNode == null)
                        {
                            _logger.LogWarning("Skipping event node: missing required nodes");
                            continue;
                        }

                        var artist = detailsNode.SelectSingleNode(".//h2");
                        var showtime = ParseEventShowtime(eventDateNode.InnerText, notesNode?.InnerText ?? string.Empty);
                        var price = ParseEventPrice(detailsNode.InnerText);

                        var newEvent = new Event
                        {
                            EventCity = City,
                            EventVenue = Venue,
                            Artist = _cleaner.Clean(artist.InnerText),
                            Showtime = showtime,
                            HasShowtime = true,
                            Price = price
                        };

                        _logger.LogInformation("Parsed event: {newEvent}", newEvent.ToString());

                        if (!Events.Exists(e => e.Equals(newEvent)))
                        {
                            Events.Add(newEvent);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to parse one event node, skipping");
                    }
                }
                _logger.LogInformation("Parsed {events.Count} events from Olympiakortteli.", Events.Count);

                return Events;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while scraping events from Olympiakortteli");
                return Events;                
            }
        }

        private static string ParseEventPrice(string eventDetails)
        {
            var priceStrings = eventDetails.Split("\n");
            var tempPriceString = string.Empty;
            var price = string.Empty;

            foreach (var ps in priceStrings)
            {
                if (ps.Contains('€'))
                {
                    tempPriceString = ps;
                    break;
                }
            }

            var priceArray = tempPriceString.Split(" ");

            for (int i = priceArray.Length - 1; i > 0; i--)
            {
                if (priceArray[i].Contains('€'))
                {
                    price = priceArray[i];
                }
                break;
            }

            return price;
        }

        private static DateTime ParseEventShowtime(string eventDate, string showTime)
        {
            var year = 0;
            var month = 0;
            var day = 0;
            var hours = 0;
            var minutes = 0;

            if (eventDate != string.Empty)
            {
                var priceStrings = eventDate.Split(" ");
                priceStrings = priceStrings[1].Split(".");

                year = int.Parse(priceStrings[2]);
                month = int.Parse(priceStrings[1]);
                day = int.Parse(priceStrings[0]);
            }

            if (showTime != string.Empty)
            {
                var timeStrings = showTime.Split(":");

                hours = int.Parse(timeStrings[0]);
                minutes = int.Parse(timeStrings[1].Split(" ")[0]);

                return new DateTime(year, month, day, hours, minutes, 0);
            }

            return new DateTime(year, month, day);
        }
    }
}