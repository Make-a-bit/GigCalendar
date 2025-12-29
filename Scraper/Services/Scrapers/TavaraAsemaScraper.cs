using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using Scraper.Models;
using Scraper.Repositories;

namespace Scraper.Services.Scrapers
{
    public class TavaraAsemaScraper : BaseScraper, IEventScraper
    {
        private readonly ICleaner _cleaner;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<TavaraAsemaScraper> _logger;
        private readonly ICityRepository _cityRepository;
        private readonly IVenueRepository _venueRepository;

        public TavaraAsemaScraper(ICleaner cleaner,
            IHttpClientFactory httpClientFactory,
            ICityRepository cityRepository,
            IVenueRepository venueRepository,
            ILogger<TavaraAsemaScraper> logger)
        {
            _cleaner = cleaner;
            _httpClientFactory = httpClientFactory;
            _cityRepository = cityRepository;
            _venueRepository = venueRepository;
            _logger = logger;

            Venue.Name = "Tavara-Asema";
            City.Name = "Tampere";
        }

        public async Task<List<Event>> ScrapeEvents()
        {
            try
            {
                _logger.LogInformation("Starting to scrape {venue} events...", Venue.Name);

                // Initialize variable
                using var client = _httpClientFactory.CreateClient();

                // Add Browser headers
                client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");

                // Fetch and parse the HTML document
                var url = "https://tavara-asema.fi/ohjelma/";
                var html = await client.GetStringAsync(url);
                Doc.LoadHtml(html);

                // Select event nodes
                var nodes = Doc.DocumentNode.SelectNodes("//div[contains(@class, 'event-feed-item__content')]");

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

                var urls = node.InnerHtml.Split("href=");
                var pageUrl = urls[1].Split('"')[1].Trim();

                if (!IsValidUrl(pageUrl))
                {
                    _logger.LogWarning("Blocked potentially malicious URL: {pageUrl}", pageUrl);
                    return newEvent;
                }

                var doc = new HtmlDocument();
                var html = await client.GetStringAsync(pageUrl);
                doc.LoadHtml(html);

                // Parse event details
                var details = doc.DocumentNode.SelectSingleNode(".//div[contains(@class, 'event-header__content')]");
                var artists = details.SelectNodes(".//h2[contains(@class, 'event-artist')]");
                var date = details.SelectSingleNode(".//span[contains(@class, 'event-date')]");
                var infoNode = details.SelectSingleNode(".//ul[contains(@class, 'event-info')]");
                var infoDetails = infoNode.SelectNodes(".//li");

                // Extract parsed details for the Event object
                newEvent.Artist = ParseArtist(artists);
                newEvent.Showtime = ParseShowtime(date.InnerText, infoNode.InnerText);
                newEvent.HasShowtime = true;
                newEvent.Price = ParsePrice(infoDetails);

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
        /// Parses the artist information from the given HTML nodes.
        /// </summary>
        /// <param name="artists">The collection of HTML nodes containing artist information.</param>
        /// <returns>A string representing the parsed artist information.</returns>
        private string ParseArtist(HtmlNodeCollection artists)
        {
            var headliner = string.Empty;

            foreach (var artist in artists)
            {
                if (headliner.Length != 0)
                {
                    headliner = headliner + ", ";
                }

                if (artist.InnerHtml.Contains("<span"))
                {
                    var headliners = artist.InnerHtml.Split("<span");
                    headliner += headliners[0].Trim();
                }
                else
                {
                    headliner += artist.InnerText;
                }
            }

            return _cleaner.Clean(headliner);
        }

        /// <summary>
        /// Parses the price information from the given HTML nodes.
        /// </summary>
        /// <param name="priceNodes">The collection of HTML nodes containing price information.</param>
        /// <returns>A string representing the parsed price information.</returns>
        private string ParsePrice(HtmlNodeCollection priceNodes)
        {
            var priceString = string.Empty;

            foreach (var node in priceNodes)
            {
                if (node.InnerText.Contains("Loppuunmyyty"))
                {
                    return "SOLD OUT!";
                }

                if (node.InnerText.Contains("Vapaa"))
                {
                    return "Vapaa pääsy!";
                }

                if (!node.InnerText.Contains("€"))
                {
                    continue;
                }

                if (priceString.Length != 0)
                {
                    priceString = priceString + " / ";
                }

                var strings = node.InnerText.Split(" ");
                priceString += strings[1].Trim();
            }

            return priceString;
        }

        /// <summary>
        /// Parses a date string in the format dd.MM and a time string in the format "HH:MM" 
        /// and returns a DateTime object.
        /// </summary>
        /// <param name="date">The date string to parse.</param>
        /// <param name="time">The time string to parse.</param>
        /// <returns>A DateTime object representing the parsed date and time.</returns>
        private DateTime ParseShowtime(string date, string time)
        {
            var now = DateTime.Now;
            int year, month, day, hours, minutes;

            var dateStrings = date.Split(" ");
            year = now.Year;
            month = int.Parse(dateStrings[1].Split(".")[1].Trim());
            day = int.Parse(dateStrings[1].Split(".")[0].Trim());

            // Adjust year if the date has already passed this year
            if (month < now.Month || month == now.Month && day < now.Day)
            {
                year = now.Year + 1;
            }

            var timeStrings = time.Split("Ovet ");

            if (timeStrings[1].Contains('–') || timeStrings[1].Contains('-'))
            {
                timeStrings = timeStrings[1].Split(new[] { '–', '-' }, StringSplitOptions.RemoveEmptyEntries);

                hours = int.Parse(timeStrings[0].Split(":")[0].Trim());
                minutes = int.Parse(timeStrings[0].Split(":")[1].Trim());
            }
            else
            {
                hours = int.Parse(timeStrings[1].Split(":")[0].Trim());
                minutes = int.Parse(timeStrings[1].Split(":")[1].Trim());
            }

            var showtime = new DateTime(year, month, day, hours, minutes, 0);
            return showtime;
        }
    }
}