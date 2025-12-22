using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using Scraper.Models;
using Scraper.Repositories;

namespace Scraper.Services.Scrapers
{
    public class SawohouseScraper : BaseScraper, IEventScraper
    {
        private readonly ICleaner _cleaner;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<SawohouseScraper> _logger;
        private readonly ICityRepository _cityRepository;
        private readonly IVenueRepository _venueRepository;

        public SawohouseScraper(ICleaner cleaner,
            IHttpClientFactory httpClientFactory,
            ICityRepository cityRepository,
            IVenueRepository venueRepository,
            ILogger<SawohouseScraper> logger)
        {
            _cleaner = cleaner;
            _httpClientFactory = httpClientFactory;
            _cityRepository = cityRepository;
            _venueRepository = venueRepository;
            _logger = logger;

            Venue.Name = "Sawohouse";
            City.Name = "Kuopio";
        }

        public async Task<List<Event>> ScrapeEvents()
        {
            try
            {
                _logger.LogInformation("Starting to scrape Sawohouse events...");

                // Initialize variables
                using var client = _httpClientFactory.CreateClient();

                // Add Browser headers
                client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");

                // Fetch and parse the HTML document
                var url = "https://sawohouseunderground.fi/keikkakalenteri/";
                var html = await client.GetStringAsync(url);
                Doc.LoadHtml(html);

                // Select event nodes
                var nodes = Doc.DocumentNode.SelectNodes("//article[contains(@class, 'mec-event-article')]");

                _logger.LogInformation("Found {events} nodes from Sawohouse.", nodes.Count);
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

                _logger.LogInformation("Parsed {events.count} events from Kulttuuritalo.", Events.Count);
                return Events;

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while scraping Sawohouse events.");
                return Events;
            }
        }

        private async Task<Event> ParseEventDetailPage(HtmlNode node, Event newEvent, HttpClient client)
        {
            try
            {
                _logger.LogInformation("Parsing event detail page...");

                var urlNode = node.SelectSingleNode(".//div[contains(@class, 'mec-event-image')]");
                var urls = urlNode.InnerHtml.Split("href=");
                var url = urls[1].Split('"')[1].Trim();

                var doc = new HtmlDocument();
                var html = await client.GetStringAsync(url);
                doc.LoadHtml(html);

                // Parse event details
                var details = doc.DocumentNode.SelectSingleNode(".//section[contains(@class, 'mec-container')]");
                var title = details.SelectSingleNode(".//h1");
                newEvent.Artist = _cleaner.Clean(title.InnerText.Trim());

                var dateNode = details.SelectSingleNode(".//span[contains(@class, 'mec-start-date-label')]");
                var timeNode = details.SelectSingleNode(".//div[contains(@class, 'single-event-time')]");
                newEvent.Showtime = ParseShowtime(dateNode.InnerText, timeNode.InnerText);
                newEvent.HasShowtime = true;

                var priceNode = details.SelectSingleNode(".//dd[contains(@class, 'event-cost')]");
                newEvent.Price = priceNode.InnerText;

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
        /// Parse the showtime from the given date and time nodes.
        /// </summary>
        /// <param name="date">The date string containing the month and day.</param>
        /// <param name="timeNode">The time string containing the event time.</param>
        /// <returns>The parsed DateTime of the event.</returns>
        private DateTime ParseShowtime(string date, string timeNode)
        {
            var now = DateTime.Now;
            int year, month, day;
            int hours = 0;
            int minutes = 0;

            // Parse event date
            var dateStrings = date.Split(' ');
            year = now.Year;
            month = ConvertMonthName(dateStrings[0]);
            day = int.Parse(dateStrings[1]);

            // Adjust year if the date has already passed this year
            if (month < now.Month || month == now.Month && day < now.Day)
            {
                year = now.Year + 1;
            }


            // Parse event showtime
            var times = timeNode.Split("\n");

            foreach (var time in times)
            {
                if (time.ToLower().Contains("pm"))
                {
                    times = time.Split(" pm");
                    times = times[0].Split(":");

                    hours = int.Parse(times[0].Trim());
                    hours += 12;

                    minutes = int.Parse(times[1].Trim());
                    break;
                }
            }

            return new DateTime(year, month, day, hours, minutes, 0);
        }

        /// <summary>
        /// Get the month number from the Finnish month name.
        /// </summary>
        /// <param name="month">The Finnish name of the month.</param>
        /// <returns>The month number (1-12).</returns>
        /// <exception cref="ArgumentException">Thrown when the month name is invalid.</exception>
        private static int ConvertMonthName(string month)
        {
            return month.Trim().ToLowerInvariant() switch
            {
                "tammi" => 1,
                "helmi" => 2,
                "maalis" => 3,
                "huhti" => 4,
                "touko" => 5,
                "kesä" => 6,
                "heinä" => 7,
                "elo" => 8,
                "syys" => 9,
                "loka" => 10,
                "marras" => 11,
                "joulu" => 12,
                _ => throw new ArgumentException($"Invalid month name: {month}")
            };
        }
    }
}
