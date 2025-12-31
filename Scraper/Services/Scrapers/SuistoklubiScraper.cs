using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using Scraper.Models;
using Scraper.Repositories;

namespace Scraper.Services.Scrapers
{
    public class SuistoklubiScraper : BaseScraper, IEventScraper
    {
        private readonly ICleaner _cleaner;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger _logger;
        private readonly ICityRepository _cityRepository;
        private readonly IVenueRepository _venueRepository;

        public SuistoklubiScraper(ICleaner cleaner,
            IHttpClientFactory httpClientFactory,
            ICityRepository cityRepository,
            IVenueRepository venueRepository,
            ILogger<SuistoklubiScraper> logger)
        {
            _cleaner = cleaner;
            _httpClientFactory = httpClientFactory;
            _cityRepository = cityRepository;
            _venueRepository = venueRepository;
            _logger = logger;

            City.Name = "Hämeenlinna";
            Venue.Name = "Suistoklubi";
        }

        public async Task<List<Event>> ScrapeEvents()
        {
            try
            {
                _logger.LogInformation("Starting to scrape {venue} events...", Venue.Name);

                // Initialize variables
                using var client = _httpClientFactory.CreateClient();

                // Fetch and parse the HTML document
                var url = "https://www.suisto.fi/";
                var html = await client.GetStringAsync(url);
                Doc.LoadHtml(html);

                // Select event nodes
                var nodes = Doc.DocumentNode.SelectNodes(".//li[contains(@class, 'tribe-events-list-widget-events')]");

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

        private async Task<Event> ParseEventDetailPage(HtmlNode node, Event newEvent, HttpClient client)
        {
            try
            {
                _logger.LogInformation("Parsing event detail page...");

                var urlStrings = node.InnerHtml.Split("href=");
                var pageUrl = urlStrings[1].Split('"')[1].Trim();

                if (!IsValidUrl(pageUrl))
                {
                    _logger.LogWarning("Blocked potentially malicious URL: {url}", pageUrl);
                    return newEvent;
                }

                var doc = new HtmlDocument();
                var html = await client.GetStringAsync(pageUrl);
                doc.LoadHtml(html);

                var eventNode = doc.DocumentNode.SelectSingleNode(".//div[contains(@class, 'tribe-events-single')]");

                // Parse event details
                var infoNode = eventNode.SelectSingleNode(".//h1");

                // Extract event details for event object
                newEvent.Artist = ParseEventArtist(infoNode);
                newEvent.Showtime = ParseShowtime(infoNode);
                newEvent.HasShowtime = true;
                newEvent.Price = ParsePrice(eventNode);

                _logger.LogInformation("Parsed event: {newEvent}", newEvent.ToString());
                return newEvent;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while parsing event detail page.");
                return newEvent;

            }
        }

        private string ParsePrice(HtmlNode priceNode)
        {
            var priceString = string.Empty;
            var nodes = priceNode.SelectNodes(".//strong");

            foreach (var node in nodes)
            {
                if (!node.InnerText.Contains('€'))
                {
                    continue;
                }

                var priceStrings = node.InnerText.Split(' ');

                foreach (var item in priceStrings)
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

            if (priceString.Length == 0)
            {
                priceString += "Vapaa pääsy!";
            }

            return priceString;
        }

        private DateTime ParseShowtime(HtmlNode node)
        {
            var now = DateTime.Now;
            int year, month, day;
            int hour = 0;
            int minute = 0;

            var strings = node.InnerText.Split(' ');
            var dayStrings = strings[1].Split('.');

            year = now.Year;
            month = int.Parse(dayStrings[1].Trim());
            day = int.Parse(dayStrings[0].Trim());

            for (int i = strings.Length - 1; i > 0; i--)
            {
                if (!strings[i].Contains('('))
                {
                    continue;
                }

                var timeStrings = strings[i].Split(":");
                hour = int.Parse(timeStrings[0].Replace("(", "").Trim());
                minute = int.Parse(timeStrings[1].Split('-')[0]
                    .Replace(")", "").Trim());

                break;
            }

            return new DateTime(year, month, day, hour, minute, 0);
        }

        private string ParseEventArtist(HtmlNode node)
        {
            var artist = string.Empty;
            var artistParts = new List<string>();
            var parts = node.InnerText
                .Split(' ', StringSplitOptions.RemoveEmptyEntries);

            // Artist name starts after date tokens (index 2)
            for (int i = 2; i < parts.Length; i++)
            {
                if (parts[i].Contains('('))
                {
                    break;
                }

                artistParts.Add(parts[i]);
            }

            return _cleaner.Clean(string.Join(' ', artistParts));
        }
    }
}
