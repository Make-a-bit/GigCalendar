using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using Scraper.Models;
using Scraper.Repositories;

namespace Scraper.Services.Scrapers
{
    public class SemifinalScraper : BaseScraper, IEventScraper
    {
        private readonly ICleaner _cleaner;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<SemifinalScraper> _logger;
        private readonly ICityRepository _cityRepository;
        private readonly IVenueRepository _venueRepository;

        public SemifinalScraper(ICleaner cleaner, 
            IHttpClientFactory httpClientFactory, 
            ICityRepository cityRepository,
            IVenueRepository venueRepository, 
            ILogger<SemifinalScraper> logger)
        {
            _cleaner = cleaner;
            _httpClientFactory = httpClientFactory;
            _cityRepository = cityRepository;
            _venueRepository = venueRepository;
            _logger = logger;

            Venue.Name = "Semifinal";
            City.Name = "Helsinki";
        }


        /// <summary>
        /// Gets the list of events from Semifinal.
        /// </summary>
        /// <returns></returns>
        public async Task<List<Event>> ScrapeEvents()
        {
            try
            {
                // Initialize variables
                var events = new List<Event>();
                HtmlDocument doc = new HtmlDocument();
                HtmlDocument innerDoc = new HtmlDocument();
                using var client = _httpClientFactory.CreateClient();

                // Fetch and parse the HTML document
                var url = "https://tavastiaklubi.fi/semifinal/?show_all=1";
                var html = await client.GetStringAsync(url);
                doc.LoadHtml(html);

                // Select event nodes
                var htmlNodes = doc.DocumentNode.SelectSingleNode("//div[contains(@class,'tiketti-list')]");
                innerDoc.LoadHtml(htmlNodes.InnerHtml);
                var nodes = innerDoc.DocumentNode.SelectNodes(".//a[contains(@class,'tiketti-list-item')]");

                // Update CityID and VenueID from database
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

                    // Clean and parse details nicely for the Event object
                    var eventTitle = _cleaner.EventCleaner(titleNode?.InnerText.Trim() ?? "Ei otsikkoa");
                    var eventDate = ParseDate(dateNode.InnerText.ToString().Trim(), startNode.InnerText.ToString().Trim());
                    var eventPrice = _cleaner.PriceCleaner(priceNode?.InnerText.Trim() ?? "Ei hintatietoa");

                    // Create new Event object with extracted details
                    var newEvent = new Event();

                    newEvent.EventVenue.Id = Venue.Id;
                    newEvent.EventVenue.Name = Venue.Name;
                    newEvent.EventCity.Id = City.Id;
                    newEvent.EventCity.Name = City.Name;
                    newEvent.Artist = eventTitle;
                    newEvent.Date = eventDate;
                    newEvent.PriceAsString = eventPrice;                  

                    // Compare if events already contains the new event.
                    // If not, add it to the list.
                    if (!events.Exists(e => e.Equals(newEvent)))
                    {
                        events.Add(newEvent);
                    }
                }

                _logger.LogInformation("Scraped {events.Count} events from Semifinal.", events.Count);

                // Return the list of events
                return events;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while scraping events from Semifinal.");
                return new List<Event>();
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