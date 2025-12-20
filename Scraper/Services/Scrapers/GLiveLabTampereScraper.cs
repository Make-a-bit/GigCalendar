using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using Scraper.Models;
using Scraper.Repositories;
using Scraper.Services;
using System.Threading.Tasks;

namespace Scraper.Services.Scrapers
{
    public class GLiveLabTampereScraper : BaseScraper, IEventScraper
    {
        private readonly ICleaner _cleaner;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger _logger;
        private readonly ICityRepository _cityRepository;
        private readonly IVenueRepository _venueRepository;

        public GLiveLabTampereScraper(ICleaner cleaner,
            IHttpClientFactory httpClientFactory,
            ICityRepository cityRepository,
            IVenueRepository venueRepository,
            ILogger<GLiveLabTampereScraper> logger)
        {
            _cleaner = cleaner;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _cityRepository = cityRepository;
            _venueRepository = venueRepository;

            Venue.Name = "G Livelab Tampere";
            City.Name = "Tampere";
        }

        public async Task<List<Event>> ScrapeEvents()
        {
            try
            {
                _logger.LogInformation("Starting to scrape G Livelab Tampere events...");

                // Initialize variables
                HtmlDocument doc = new HtmlDocument();
                using var client = _httpClientFactory.CreateClient();
                int skippedCount = 0;

                // Fetch and parse the HTML document
                var url = "https://glivelab.fi/tampere/?show_all=1";
                var html = await client.GetStringAsync(url);
                doc.LoadHtml(html);

                // Select event nodes
                var htmlNodes = doc.DocumentNode.SelectSingleNode("//div[contains(@class, 'segmented-control-tab')]");
                doc.LoadHtml(htmlNodes.InnerHtml);
                var nodes = doc.DocumentNode.SelectNodes(".//li[contains(@class, 'item')]");

                _logger.LogInformation("Found {events.count} events from G Livelab Tampere.", nodes.Count);
                _logger.LogInformation("Starting to parse event details...");
                // Update CityID and VenueID from database
                City.Id = await  _cityRepository.GetCityIdAsync(City.Name);
                Venue.Id = await _venueRepository.GetVenueIdAsync(Venue.Name, City.Id);

                // Iterate through each event node and extract details
                foreach (var node in nodes)
                {
                    // Skip sticky events and advertisements
                    if (node.InnerHtml.Contains("stickyevent") || node.InnerHtml.Contains("advert"))
                    {
                        skippedCount++;
                        continue;
                    }

                    // Extract event details
                    var newEvent = new Event
                    {
                        EventCity = City,
                        EventVenue = Venue,
                    };

                    // Parse event detail page for more information
                    newEvent = await ParseEventDetailPage(node.InnerHtml, newEvent);

                    // Compare if events already contains the new event. If not, add it to the list.
                    if (!Events.Exists(e => e.Equals(newEvent)))
                    {
                        Events.Add(newEvent);
                    }
                }

                _logger.LogInformation("Scraped {events.count} events from G Livelab Tampere. Skipped {skipped} ads.", Events.Count, skippedCount);
                return Events;
            }
            catch (Exception ex) 
            {
                _logger.LogError(ex, "An error occurred while scraping G Livelab Tampere events.");
                return new List<Event>();
            }
        }

        /// <summary>
        /// Parses the event detail page to extract event information.
        /// </summary>
        /// <param name="text">HTML content of the event detail page</param>
        /// <param name="newEvent">Event object to populate with parsed details</param>
        /// <returns>Populated Event object with details extracted from the page</returns>
        private async Task<Event> ParseEventDetailPage(string text, Event newEvent)
        {
            try
            {
                _logger.LogInformation("Parsing event detail page...");

                var strings = text.Split('"');
                var pageUrl = strings[1];

                var doc = new HtmlDocument();
                using var client = _httpClientFactory.CreateClient();
                var html = await client.GetStringAsync(pageUrl);
                doc.LoadHtml(html);

                var nodes = doc.DocumentNode.SelectNodes("//article[contains(@class, 'page')]");

                // Extract event details
                foreach (var node in nodes)
                {
                    var titleNode = node.SelectSingleNode(".//h1");
                    newEvent.Artist = titleNode.InnerText.Split(',')[0].Trim();

                    var dateNode = node.SelectSingleNode(".//div[contains(@class, 'datetime')]");
                    newEvent.Date = ParseDate(dateNode?.InnerText.Trim() ?? string.Empty);
                    newEvent.HasShowtime = true;

                    var priceNode = node.SelectNodes(".//span[contains(@class, 'prices')]");
                    newEvent.Price = ParseEventPrices(priceNode);
                }

                _logger.LogInformation("Parsed event: {newEvent}", newEvent.ToString());
                return newEvent;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while parsing event detail page.");
                return newEvent;
            }
        }

        /// <summary>
        /// Parses event prices from the given HTML nodes.
        /// </summary>
        /// <param name="nodes">HTML nodes containing price information</param>
        /// <returns>Concatenated string of event prices</returns>
        private string ParseEventPrices(HtmlNodeCollection nodes)
        {
            var priceString = string.Empty;

            if (nodes == null)
            {
                return priceString;
            }

            foreach (var node in nodes)
            {
                if (priceString != string.Empty)
                {
                    priceString += " / ";
                }

                var price = _cleaner.Clean(node.InnerText);
                priceString += price;
            }

            return priceString;
        }

        /// <summary>
        /// Parses a date string in the format dd.MM and returns a DateTime object.
        /// </summary>
        /// <param name="date">The date string to parse.</param>
        /// <returns>A DateTime object representing the parsed date.</returns>
        private static DateTime ParseDate(string date)
        {
            var now = DateTime.Now;

            var dateStrings = date.Split('\n');

            int year = now.Year;
            int month = int.Parse(dateStrings[0].Split('.')[1].Trim());
            int day = int.Parse(dateStrings[0].Split('.')[0].Trim());
            int hours = int.Parse(dateStrings[2].Split(':')[0].Trim());
            int minutes = int.Parse(dateStrings[2].Split(':')[1].Trim());

            // Adjust year if the date has already passed this year
            if (month < now.Month || month == now.Month && day < now.Day)
            {
                year = now.Year + 1;
            }

            return new DateTime(year, month, day, hours, minutes, 0);
        }
    }
}