using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using Scraper.Models;
using Scraper.Repositories;
using static System.Net.WebRequestMethods;

namespace Scraper.Services.Scrapers
{
    public class KuudesLinjaScraper : BaseScraper, IEventScraper
    {
        private readonly ICleaner _cleaner;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<KuudesLinjaScraper> _logger;
        private readonly ICityRepository _cityRepository;
        private readonly IVenueRepository _venueRepository;

        public KuudesLinjaScraper(ICleaner cleaner,
            IHttpClientFactory httpClientFactory,
            ICityRepository cityRepository,
            IVenueRepository venueRepository,
            ILogger<KuudesLinjaScraper> logger)
        {
            _cleaner = cleaner;
            _httpClientFactory = httpClientFactory;
            _cityRepository = cityRepository;
            _venueRepository = venueRepository;
            _logger = logger;

            Venue.Name = "Kuudes Linja";
            City.Name = "Helsinki";
        }

        public async Task<List<Event>> ScrapeEvents()
        {
            try
            {
                _logger.LogInformation("Starting to scrape Kuudes Linja events...");

                // Initialize variables
                using var client = _httpClientFactory.CreateClient();

                // Fetch and parse the HTML document
                var url = "https://www.kuudeslinja.com/";
                var html = await client.GetStringAsync(url);
                Doc.LoadHtml(html);

                // Select event nodes
                var nodes = Doc.DocumentNode.SelectNodes(".//article[contains(@class, 'event')]");

                _logger.LogInformation("Found {events.count} events from Kuudes Linja.", nodes.Count);
                _logger.LogInformation("Starting to parse event details...");

                // Update CityId and VenueId from database
                City.Id = await _cityRepository.GetCityIdAsync(City.Name);
                Venue.Id = await _venueRepository.GetVenueIdAsync(Venue.Name, City.Id);

                // Iterate trough each event node and extract details
                foreach (var node in nodes)
                {
                    var newEvent = new Event
                    {
                        EventCity = City,
                        EventVenue = Venue,
                    };

                    // Parse event details
                    var titleNode = node.SelectSingleNode(".//div[contains(@class, 'title')]");
                    var dateNode = node.SelectSingleNode(".//div[contains(@class, 'pvm')]");
                    var infoNode = node.SelectSingleNode(".//div[contains(@class, 'info')]");

                    // Extract parsed details for Event object
                    newEvent.Artist = CleanEventTitle(titleNode.InnerText.Trim());
                    newEvent.Date = ParseDate(dateNode.InnerText, infoNode.InnerText.Trim());
                    newEvent.HasShowtime = true;
                    newEvent.Price = ParsePrice(infoNode.InnerText);

                    // Compare if events already contains the new event.
                    // If not, add it to the list.
                    if (!Events.Exists(e => e.Equals(newEvent)))
                    {
                        Events.Add(newEvent);
                    }
                }

                _logger.LogInformation("Parsed {events.count} events from Kuudes Linja.", Events.Count);

                // Return the list of events
                return Events;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while scraping events from Kuudes Linja.");
                return Events;
            }
        }

        /// <summary>
        /// Cleans the event title by removing unwanted prefixes and trimming whitespace.
        /// </summary>
        /// <param name="title">The raw event title string to be cleaned.</param>
        /// <returns>A cleaned event title string.</returns>
        private string CleanEventTitle(string title)
        {
            if (title.Contains("KONSERTTI:"))
            {
                var titles = title.Split("KONSERTTI:");
                title = titles[1].Trim();
            }

            return _cleaner.Clean(title.Trim());
        }

        /// <summary>
        /// Parses the event price from the given info node string.
        /// </summary>
        /// <param name="infoNode">The raw info node string containing price information.</param>
        /// <returns>A parsed price string.</returns>
        private static string ParsePrice(string infoNode)
        {
            var priceString = string.Empty;
            var infoStrings = infoNode.Split('\n');
            
            foreach (var info in infoStrings)
            {
                if (info.Contains(" €"))
                {
                    var priceInfos = info.Split('.');
                    priceString = priceInfos[0].Trim();
                    priceString = priceString.Replace(" €", "€");

                    break;
                }
            }
            return priceString;
        }

        /// <summary>
        /// Parses the event date and time from the given strings.
        /// </summary>
        /// <param name="date">The raw date string.</param>
        /// <param name="time">The raw time string.</param>
        /// <returns>A DateTime object representing the event date and time.</returns>
        private static DateTime ParseDate(string date, string time)
        {
            var now = DateTime.Now;
            int year, month, day, hours, minutes = 0;

            // Parse event date
            var dateStrings = date.Split(' ');
            dateStrings = dateStrings[1].Split('.');

            year = now.Year;
            month = int.Parse(dateStrings[1].Trim());
            day = int.Parse(dateStrings[0].Trim());

            // Adjust year if the date has already passed this year
            if (month < now.Month || month == now.Month && day < now.Day)
            {
                year = now.Year + 1;
            }

            // Parse event time
            var timeStrings = time.Split("Ovet klo ");
            timeStrings = timeStrings[1].Split("\n");
            var showtime = timeStrings[0].Split("–");

            if (showtime[0].Length == 2)
            {
                hours = int.Parse(showtime[0]);
            }
            else
            {
                hours = int.Parse(showtime[0].Split(':')[0]);
                minutes = int.Parse(showtime[0].Split(':')[1]);
            }

            return new DateTime(year, month, day, hours, minutes, 0);
        }
    }
}
