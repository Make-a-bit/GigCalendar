using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using Scraper.Models;
using Scraper.Repositories;
using System.Net;

namespace Scraper.Services.Scrapers
{
    public abstract class GLiveLabBaseScraper : BaseScraper, IEventScraper
    {
        protected readonly ICleaner _cleaner;
        protected readonly IHttpClientFactory _httpClientFactory;
        protected readonly ILogger _logger;
        protected readonly ICityRepository _cityRepository;
        protected readonly IVenueRepository _venueRepository;

        protected abstract string ScrapeUrl { get; }

        protected GLiveLabBaseScraper(
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
                int skippedCount = 0;

                // Fetch and parse the HTML document
                var html = await client.GetStringAsync(ScrapeUrl);
                Doc.LoadHtml(html);

                // Select event nodes
                var htmlNodes = Doc.DocumentNode.SelectSingleNode("//div[contains(@class, 'segmented-control-tab')]");
                Doc.LoadHtml(htmlNodes.InnerHtml);
                var nodes = Doc.DocumentNode.SelectNodes(".//li[contains(@class, 'item')]");

                _logger.LogInformation("Found {events.count} event pages from {venue}.", nodes.Count, Venue.Name);
                _logger.LogInformation("Starting to parse event pages...");

                // Update CityID and VenueID from database
                City.Id = await _cityRepository.GetCityIdAsync(City.Name);
                Venue.Id = await _venueRepository.GetVenueIdAsync(Venue.Name, City.Id);

                // Iterate through each event node and extract details
                int eventIndex = 0;
                foreach (var node in nodes)
                {
                    eventIndex++;

                    // Skip sticky events and advertisements
                    if (node.InnerHtml.Contains("stickyevent") || node.InnerHtml.Contains("advert"))
                    {
                        _logger.LogInformation("Skipping an ad or other non-event page...");
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
                    newEvent = await ParseEventDetailPage(node.InnerHtml, newEvent, client);

                    // Compare if events already contains the new event. If not, add it to the list.
                    if (!Events.Exists(e => e.Equals(newEvent)))
                    {
                        Events.Add(newEvent);
                    }

                    // Add delay before next iteration
                    if (eventIndex < nodes.Count)
                    {
                        var delayMs = Delay.CalculateSeconds();
                        _logger.LogInformation("Waiting {delay}ms before next request ({current}/{total})...", delayMs, eventIndex + 1, nodes.Count);

                        await Task.Delay(delayMs);
                    }
                }

                _logger.LogInformation("Parsed {events.count} events from {venue}. Skipped {skipped} pages.",
                    Events.Count, Venue.Name, skippedCount);

                return Events;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while scraping {venue} events.", Venue.Name);

                return Events;
            }
        }


        /// <summary>
        /// Parses the event detail page to extract event information.
        /// </summary>
        /// <param name="text">HTML content of the event detail page</param>
        /// <param name="newEvent">Event object to populate with parsed details</param>
        /// <returns>Populated Event object with details extracted from the page</returns>
        private async Task<Event> ParseEventDetailPage(string text, Event newEvent, HttpClient client)
        {
            try
            {
                _logger.LogInformation("Parsing event page...");

                var strings = text.Split('"');
                var pageUrl = strings[1];

                if (!IsValidUrl(pageUrl))
                {
                    _logger.LogWarning("Blocked potentially malicious URL: {url}", pageUrl);
                    return newEvent;
                }

                var doc = new HtmlDocument();
                var html = await client.GetStringAsync(pageUrl);
                doc.LoadHtml(html);

                var nodes = doc.DocumentNode.SelectNodes("//article[contains(@class, 'page')]");

                // Extract event details
                foreach (var node in nodes)
                {
                    var titleNode = node.SelectSingleNode(".//h1");
                    newEvent.Artist = _cleaner.Clean(titleNode.InnerText.Split(',')[0].Trim());

                    var showtimeNode = node.SelectSingleNode(".//div[contains(@class, 'datetime')]");
                    newEvent.Showtime = ParseShowtime(showtimeNode?.InnerText.Trim() ?? string.Empty);
                    newEvent.HasShowtime = true;

                    var priceNode = node.SelectNodes(".//span[contains(@class, 'prices')]");
                    newEvent.Price = _cleaner.CleanPrice(priceNode);
                }

                _logger.LogInformation("Parsed event: {newEvent}", newEvent.ToString());
                return newEvent;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while parsing event page.");
                return newEvent;
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
