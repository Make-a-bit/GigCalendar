using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using Scraper.Models;
using Scraper.Repositories;

namespace Scraper.Services.Scrapers
{
    public class HouseOfRockScraper : BaseScraper, IEventScraper
    {
        private readonly ICleaner _cleaner;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger _logger;
        private readonly ICityRepository _cityRepository;
        private readonly IVenueRepository _venueRepository;

        public HouseOfRockScraper(ICleaner cleaner,
            IHttpClientFactory httpClientFactory,
            ICityRepository cityRepository,
            IVenueRepository venueRepository,
            ILogger<HouseOfRockScraper> logger)
        {
            _cleaner = cleaner;
            _httpClientFactory = httpClientFactory;
            _cityRepository = cityRepository;
            _venueRepository = venueRepository;
            _logger = logger;

            City.Name = "Kouvola";
            Venue.Name = "House of rock";
        }

        public async Task<List<Event>> ScrapeEvents()
        {
            try
            {
                _logger.LogInformation("Starting to scrape {venue} events...", Venue.Name);

                // Initialize variables
                using var client = _httpClientFactory.CreateClient();

                // Add Browser headers
                client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");

                // Fetch and parse the HTML document
                var url = "https://houseofrockbar.fi/tapahtumat/";
                var html = await client.GetStringAsync(url);
                Doc.LoadHtml(html);

                // Select event nodes
                var nodes = Doc.DocumentNode.SelectNodes(".//div[contains(@class, 'em-item-info')]");

                _logger.LogInformation("Found {events.count} nodes from {venue}.", nodes.Count, Venue.Name);
                _logger.LogInformation("Starting to parse event details...");

                // Update CityId and VenueId from database
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

                    newEvent = await ParseEventDetailPage(node, newEvent, client);

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

        private async Task<Event> ParseEventDetailPage(HtmlNode text, Event newEvent, HttpClient client)
        {
            try
            {
                _logger.LogInformation("Parsing event detail page...");

                var strings = text.InnerHtml.Split("href=");
                var pageUrl = strings[1].Split('"')[1].Trim();

                if (!IsValidUrl(pageUrl))
                {
                    _logger.LogWarning("Blocked potentially malicious URL: {url}", pageUrl);
                    return newEvent;
                }

                var doc = new HtmlDocument();
                var html = await client.GetStringAsync(pageUrl);
                doc.LoadHtml(html);

                var eventNode = doc.DocumentNode.SelectSingleNode(".//div[contains(@class, 'zak-primary')]");

                // Parse event details
                var titleNode = text.SelectSingleNode(".//h3");
                var dateNode = eventNode.SelectSingleNode(".//div[contains(@class, 'em-event-date')]");
                var dateStrings = dateNode.InnerText.Split("&nbsp");
                var timeNode = eventNode.SelectSingleNode(".//div[contains(@class, 'em-event-time')]");
                var priceNode = eventNode.SelectSingleNode(".//section[contains(@class, 'em-event-content')]");

                // Extract event details for event object
                newEvent.Artist = _cleaner.Clean(titleNode.InnerText.Trim());
                newEvent.Showtime = ParseShowtime(dateStrings[0], timeNode.InnerText);
                newEvent.HasShowtime = true;
                newEvent.Price = ParsePrice(priceNode);

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
        /// Parses the price information from the HTML node.
        /// </summary>
        /// <param name="price">The HTML node containing price information.</param>
        /// <returns>A string representing the parsed price.</returns>
        private string ParsePrice(HtmlNode price)
        {
            var priceString = string.Empty;
            var nodes = price.SelectNodes(".//p");

            foreach (var node in nodes)
            {
                if (node.InnerHtml == "&nbsp;")
                {
                    break;
                }

                if (!node.InnerText.Contains('€'))
                {
                    continue;
                }

                var strings = node.InnerText.Split(' ');

                foreach (var item in strings)
                {
                    if (!item.Contains('€'))
                    {
                        continue;
                    }

                    if (priceString.Length != 0)
                    {
                        priceString += " / ";
                    }

                    priceString += item;
                }
            }

            return priceString;
        }

        /// <summary>
        /// Parses the showtime from the given date and time strings.
        /// </summary>
        /// <param name="date">The date string in the format "dd.MM.yyyy".</param>
        /// <param name="time">The time string in the format "HH:mm".</param>
        /// <returns>A DateTime object representing the showtime.</returns>
        private DateTime ParseShowtime(string date, string time)
        {
            int year, month, day, hour, minute;

            var dateStrings = date.Split(".");
            year = int.Parse(dateStrings[2].Trim());
            month = int.Parse(dateStrings[1].Trim());
            day = int.Parse(dateStrings[0].Trim());

            var timeStrings = time.Split(":");
            hour = int.Parse(timeStrings[0].Trim());
            minute = int.Parse(timeStrings[1].Trim());

            return new DateTime(year, month, day, hour, minute, 0);
        }
    }
}
