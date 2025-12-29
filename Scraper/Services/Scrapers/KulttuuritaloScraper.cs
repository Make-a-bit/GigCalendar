using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using Scraper.Models;
using Scraper.Repositories;

namespace Scraper.Services.Scrapers
{
    public class KulttuuritaloScraper : BaseScraper, IEventScraper
    {
        private readonly ICleaner _cleaner;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger _logger;
        private readonly ICityRepository _cityRepository;
        private readonly IVenueRepository _venueRepository;

        public KulttuuritaloScraper(ICleaner cleaner,
            IHttpClientFactory httpClientFactory,
            ICityRepository cityRepository,
            IVenueRepository venueRepository,
            ILogger<KulttuuritaloScraper> logger)
        {
            _cleaner = cleaner;
            _httpClientFactory = httpClientFactory;
            _cityRepository = cityRepository;
            _venueRepository = venueRepository;
            _logger = logger;

            City.Name = "Helsinki";
            Venue.Name = "Kulttuuritalo";
        }

        public async Task<List<Event>> ScrapeEvents()
        {
            try
            {
                _logger.LogInformation("Starting to scrape {venue} events...", Venue.Name);

                //Initialize variables
                using var client = _httpClientFactory.CreateClient();

                // Add Browser headers
                client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");

                // Fetch and parse the HTML document
                var url = "https://kulttuuritalo.fi/tapahtumat/";

                _logger.LogDebug("Initial delay before first request...");
                await Task.Delay(Random.Shared.Next(1000, 2000));

                var html = await client.GetStringAsync(url);
                Doc.LoadHtml(html);

                // Select event nodes
                var htmlNodes = Doc.DocumentNode.SelectSingleNode("//div[contains(@class, 'pp-content-posts')]");
                Doc.LoadHtml(htmlNodes.InnerHtml);
                var nodes = Doc.DocumentNode.SelectNodes(".//div[contains(@class, 'tapahtuma type-tapahtuma')]");

                _logger.LogInformation("Found {events.count} nodes from {venue}.", nodes.Count, Venue.Name);
                _logger.LogInformation("Starting to parse event details...");

                // Update CityID and VenueId from database
                City.Id = await _cityRepository.GetCityIdAsync(City.Name);
                Venue.Id = await _venueRepository.GetVenueIdAsync(Venue.Name, City.Id);

                // Iterate trough each event node and extract details
                int eventIndex = 0;
                foreach (var node in nodes)
                {
                    eventIndex++;

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

                _logger.LogInformation("Parsed {count} events from {venue}.", Events.Count, Venue.Name);
                return Events;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while scraping {venue} events.", Venue.Name);
                return Events;
            }
        }


        /// <summary>
        /// Parses the event detail page for more information.
        /// </summary>
        /// <param name="text">The HTML content of the event node.</param>
        /// <param name="newEvent">The event object to be populated with details.</param>
        /// <param name="client">The HTTP client used for making requests.</param>
        /// <returns>The populated event object with details from the event page.</returns>
        private async Task<Event> ParseEventDetailPage(string text, Event newEvent, HttpClient client)
        {
            try
            {
                _logger.LogInformation("Parsing event detail page...");

                var strings = text.Split("href=");
                var pageUrl = strings[1].Split('"')[1];

                if (!IsValidUrl(pageUrl))
                {
                    _logger.LogWarning("Blocked potentially malicious URL: {url}", pageUrl);
                    return newEvent;
                }

                var doc = new HtmlDocument();
                var html = await client.GetStringAsync(pageUrl);
                doc.LoadHtml(html);

                var nodes = doc.DocumentNode.SelectNodes("//div[contains(@class, 'fl-page-content')]");

                foreach (var node in nodes)
                {
                    // Parse event details
                    var titleNode = node.SelectSingleNode(".//h1");
                    var dateNode = node.SelectSingleNode(".//h2");
                    var timeNode = node.SelectSingleNode(".//div[contains(@class, 'fl-callout-text-wrap')]");
                    var pricenodes = node.SelectNodes("//div[contains(@class, 'fl-module fl-module-rich-text fl-rich-text')]");

                    // Extract event details for event object
                    newEvent.Artist = _cleaner.Clean(titleNode.InnerText.Trim());
                    newEvent.Showtime = ParseShowtime(dateNode.InnerText, timeNode.InnerText);
                    newEvent.HasShowtime = true;
                    newEvent.Price = ParseEventPrice(pricenodes);
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
        /// Parses the event price from the given HTML nodes.
        /// </summary>
        /// <param name="nodes">The collection of HTML nodes to search for price information.</param>
        /// <returns>A string representing the event price, or an empty string if not found.</returns>
        private string ParseEventPrice(HtmlNodeCollection nodes)
        {
            var priceString = string.Empty;

            if (nodes == null)
            {
                return priceString;
            }

            foreach (var node in nodes)
            {
                if (node.InnerText.Contains('€'))
                {
                    priceString = node.InnerText.Trim()
                        .Replace("€€", "€")
                        .Replace(" €", "€");

                    priceString = _cleaner.ReplacePrefixes(priceString);
                    break;
                }
            }

            return priceString;
        }


        /// <summary>
        /// Parses the showtime from the given date and time strings.
        /// </summary>
        /// <param name="date">The date string to parse.</param>
        /// <param name="time">The time string to parse.</param>
        /// <returns>A DateTime object representing the parsed showtime.</returns>
        private static DateTime ParseShowtime(string date, string time)
        {
            var now = DateTime.Now;

            var dateStrings = date.Split('.');
            var timeStrings = time.Split(": ");
            var showtime = timeStrings[1].Split('\n')[0];

            int year = int.Parse(dateStrings[2]);
            int month = int.Parse(dateStrings[1]);
            int day = int.Parse(dateStrings[0]);

            int hours = 0;
            int minutes = 0;

            if (showtime.Contains(':'))
            {
                hours = int.Parse(showtime.Split(':')[0].Trim());
                minutes = int.Parse(showtime.Split(':')[1].Trim());
            }
            else if (showtime.Contains('.'))
            {
                hours = int.Parse(showtime.Split('.')[0].Trim());
                minutes = int.Parse(showtime.Split('.')[1].Trim());
            }

            return new DateTime(year, month, day, hours, minutes, 0);
        }
    }
}
