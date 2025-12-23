using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using Scraper.Models;
using Scraper.Repositories;

namespace Scraper.Services.Scrapers
{
    public class TavastiaScraper : BaseScraper, IEventScraper
    {
        private readonly ICleaner _cleaner;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<TavastiaScraper> _logger;
        private readonly ICityRepository _cityRepository;
        private readonly IVenueRepository _venueRepository;

        public TavastiaScraper(ICleaner cleaner,
            IHttpClientFactory httpClientFactory, 
            ICityRepository cityRepository,
            IVenueRepository venueRepository, 
            ILogger<TavastiaScraper> logger)
        {
            _cleaner = cleaner;
            _httpClientFactory = httpClientFactory;
            _cityRepository = cityRepository;
            _venueRepository = venueRepository;
            _logger = logger;

            Venue.Name = "Tavastiaklubi";
            City.Name = "Helsinki";
        }


        /// <summary>
        /// Gets the list of events from Tavastiaklubi.
        /// </summary>
        /// <returns></returns>
        public async Task<List<Event>> ScrapeEvents()
        {
            try
            {
                _logger.LogInformation("Starting to scrape Tavastiaklubi events...");

                // Initialize variables
                using var client = _httpClientFactory.CreateClient();

                // Fetch and parse the HTML document
                var url = "https://tavastiaklubi.fi/?show_all=1";
                var html = await client.GetStringAsync(url);
                Doc.LoadHtml(html);

                // Select event nodes
                var htmlNodes = Doc.DocumentNode.SelectSingleNode("//div[contains(@class,'tiketti-list')]");
                Doc.LoadHtml(htmlNodes.InnerHtml);
                var nodes = Doc.DocumentNode.SelectNodes(".//a[contains(@class,'tiketti-list-item')]");

                _logger.LogInformation("Found {events.count} nodes from Tavastiaklubi", nodes.Count);
                _logger.LogInformation("Starting to parse event details...");

                // Check location ID
                City.Id = await _cityRepository.GetCityIdAsync(City.Name);
                Venue.Id = await _venueRepository.GetVenueIdAsync(Venue.Name, City.Id);

                // Iterate through each event node and extract details
                foreach (var n in nodes)
                {
                    // Extract event details
                    var titleNode = n.SelectSingleNode(".//h3");
                    var dateNode = n.SelectSingleNode(".//div[contains(@class, 'date')]");
                    var startNode = n.SelectSingleNode(".//div[contains(@class, 'timetable')]");
                    var priceNode = n.SelectSingleNode(".//div[contains(@class, 'tickets')]");
                    var priceText = priceNode.InnerText
                        .Replace(" ", "")
                        .Replace("Loppuunmyyty", "SOLD OUT!")
                        .Trim();
                    var prices = priceText.Split("/");

                    if (prices.Length > 1)
                    {
                        priceText = _cleaner.CleanPrice(prices);
                    }

                    // Clean and parse details nicely for the Event object
                    var eventTitle = _cleaner.Clean(titleNode?.InnerText.Trim() ?? "Ei otsikkoa");
                    var eventDate = ParseDate(dateNode.InnerText.ToString().Trim(), startNode.InnerText.ToString().Trim());

                    // Create new Event object with extracted details
                    var newEvent = new Event
                    {
                        EventCity = City,
                        EventVenue = Venue,
                        Artist = eventTitle,
                        Showtime = eventDate,
                        HasShowtime = true,
                        Price = priceText,
                    };

                    _logger.LogInformation("Parsed event: {newEvent}", newEvent.ToString());

                    // Compare if events already contains the new event. If not, add it to the list.
                    if (!Events.Exists(e => e.Equals(newEvent)))
                    {
                        Events.Add(newEvent);
                    }
                }

                _logger.LogInformation("Parsed {events.Count} events from Tavastia.", Events.Count);

                // Return the list of events
                return Events;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while scraping events from Tavastiaklubi.");
                return Events;
            }
        }


        /// <summary>
        /// Parses a date string in the format dd.MM and returns a DateTime object.
        /// </summary>
        /// <param name="date">The date string to parse.</param>
        /// <returns>A DateTime object representing the parsed date.</returns>
        private static DateTime ParseDate(string date, string time)
        {
            var now = DateTime.Now;

            var dateStrings = date.Split('.');
            var timeStrings = time.Split('.');

            int year = now.Year;
            int month = int.Parse(dateStrings[1]);
            int day = int.Parse(dateStrings[0].Split(' ')[1]);
            int hours = 0;
            int minutes = 0;

            // Parse time if available
            if (!string.IsNullOrEmpty(time) && !string.IsNullOrWhiteSpace(time))
            {
                hours = int.Parse(timeStrings[0]);
                minutes = int.Parse(timeStrings[1].Split('\n')[0]);
            }

            // Adjust year if the date has already passed this year
            if (month < now.Month || month == now.Month && day < now.Day)
            {
                year = now.Year + 1;
            }

            return new DateTime(year, month, day, hours, minutes, 0);
        }
    }
}