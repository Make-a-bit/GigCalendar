using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using Scraper.Models;
using Scraper.Repositories;

namespace Scraper.Services.Scrapers
{
    public class MusaklubiScraper : BaseScraper, IEventScraper
    {
        private readonly ICleaner _cleaner;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<MusaklubiScraper> _logger;
        private readonly ICityRepository _cityRepository;
        private readonly IVenueRepository _venueRepository;

        public MusaklubiScraper(ICleaner cleaner, 
            IHttpClientFactory httpClientFactory,
            ICityRepository cityRepository,
            IVenueRepository venueRepository, 
            ILogger<MusaklubiScraper> logger)
        {
            _cleaner = cleaner;
            _httpClientFactory = httpClientFactory;
            _cityRepository = cityRepository;
            _venueRepository = venueRepository;
            _logger = logger;

            Venue.Name = "Möysän Musaklubi";
            City.Name = "Lahti";
        }

        /// <summary>
        /// Gets the list of events from Musaklubi
        /// </summary>
        /// <returns></returns>
        public async Task<List<Event>> ScrapeEvents()
        {
            try
            {
                _logger.LogInformation("Starting to scrape Musaklubi events...");

                // Initialize variables
                using var client = _httpClientFactory.CreateClient();

                // Fetch and parse the HTML document
                var url = "https://moysa.fi/keikat/";
                var html = await client.GetStringAsync(url);
                Doc.LoadHtml(html);

                // Select event nodes
                var htmlNodes = Doc.DocumentNode.SelectSingleNode("//div[contains(@class,'mec-event-list-classic')]");
                Doc.LoadHtml(htmlNodes.InnerHtml);
                var nodes = Doc.DocumentNode.SelectNodes(".//article[contains(@class,'mec-event-article')]");

                _logger.LogInformation("Found {events.count} events from Möysän Musaklubi.", nodes.Count);
                _logger.LogInformation("Starting to parse event details...");

                // Update CityID and VenueID from database
                City.Id = await _cityRepository.GetCityIdAsync(City.Name);
                Venue.Id = await _venueRepository.GetVenueIdAsync(Venue.Name, City.Id);

                // Iterate through each event node and extract details
                int eventIndex = 0;
                foreach (var node in nodes)
                {
                    eventIndex++;

                    // Initialize new Event object
                    var newEvent = new Event
                    {
                        EventCity = City,
                        EventVenue = Venue,
                    };

                    // Parse event detail page and extract event details
                    newEvent = await ParseEventDetailsPage(node.InnerHtml, newEvent, client);

                    // Compare if events already contains the new event.
                    // If not, add it to the list.
                    if (!Events.Exists(e => e.Equals(newEvent)))
                    {
                        Events.Add(newEvent);
                    }

                    // Add delay before next iteration
                    if (eventIndex < nodes.Count)
                    {
                        var delayMs = Delay.Calculate();
                        _logger.LogInformation("Waiting {delay}ms before next request ({current}/{total})...", delayMs, eventIndex + 1, nodes.Count);

                        await Task.Delay(delayMs);
                    }
                }

                _logger.LogInformation("Parsed {events.Count} events from Musaklubi.", Events.Count);

                // Return the list of events
                return Events;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while scraping events from Musaklubi.");
                return Events;
            }
        }


        private async Task<Event> ParseEventDetailsPage(string text, Event newEvent, HttpClient client)
        {
            try
            {
                _logger.LogInformation("Parsing event detail page...");

                var strings = text.Split("href=");
                var eventUrl = strings[2].Split('"')[1];

                var doc = new HtmlDocument();
                var html = await client.GetStringAsync(eventUrl);
                doc.LoadHtml(html);

                var nodes = doc.DocumentNode.SelectNodes("//article[contains(@class, 'event')]");

                foreach (var node in nodes)
                {
                    var titleNode = node.SelectSingleNode(".//h1");
                    newEvent.Artist = _cleaner.Clean(titleNode.InnerText.Trim());

                    var detailNodes = node.SelectNodes(".//dd");
                    DateOnly date = DateOnly.Parse(detailNodes[0].InnerText);
                    TimeOnly showtime = TimeOnly.Parse(detailNodes[1].InnerText);
                    DateTime eventDate = new DateTime(date, showtime);
                    
                    newEvent.Date = eventDate;
                    newEvent.HasShowtime = true;

                    if (detailNodes.Count > 2)
                    {
                        newEvent.Price = detailNodes[2].InnerText
                            .Replace(".", ",");
                    }
                    else
                    {
                        newEvent.Price = "SOLD OUT!";
                    }
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
    }
}
