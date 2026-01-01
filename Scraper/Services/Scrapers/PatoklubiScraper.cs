using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using Scraper.Models;
using Scraper.Repositories;

namespace Scraper.Services.Scrapers
{
    public class PatoklubiScraper : BaseScraper, IEventScraper
    {
        private readonly ICleaner _cleaner;
        private readonly ILogger _logger;
        private readonly ICityRepository _cityRepository;
        private readonly IVenueRepository _venueRepository;
        private static readonly string[] _weekDays = 
        {
            "ma",
            "ti",
            "ke",
            "to",
            "pe",
            "la",
            "su"
        };

        public PatoklubiScraper(ICleaner cleaner,
            ICityRepository cityRepository,
            IVenueRepository venueRepository,
            ILogger<PatoklubiScraper> logger)
        {
            _cleaner = cleaner;
            _cityRepository = cityRepository;
            _venueRepository = venueRepository;
            _logger = logger;

            City.Name = "Kuusankoski";
            Venue.Name = "Patoklubi";
        }

        public async Task<List<Event>> ScrapeEvents()
        {
            try
            {
                _logger.LogInformation("Starting to scrape {venue} events...", Venue.Name);

                using var playwright = await Playwright.CreateAsync();
                await using var browser = await playwright.Chromium.LaunchAsync(new()
                {
                    Headless = true
                });

                // Navigate to the events page and wait for content to load
                var page = await browser.NewPageAsync();
                await page.GotoAsync("https://www.patoklubi.fi/tapahtumat", new()
                {
                    Timeout = 60000,
                    WaitUntil = WaitUntilState.DOMContentLoaded
                });

                // Wait for iframe to load and get its content
                _logger.LogInformation("Waiting for iframe...");
                var frameHandle = await page.WaitForSelectorAsync("iframe[title='Events Calendar']", new()
                {
                    Timeout = 60000
                });
                var frameContent = await frameHandle.ContentFrameAsync();

                // Wait for events to load in iframe and select event elements
                _logger.LogInformation("Waiting for events to load in iframe...");
                await frameContent.WaitForSelectorAsync(".cl-view-agenda__event", new()
                {
                    Timeout = 60000,
                });
                await Task.Delay(3000);

                // Select all event elements
                var eventElements = await frameContent.QuerySelectorAllAsync(".cl-view-agenda__event");
                _logger.LogInformation("Found {count} events from {venue}", eventElements.Count, Venue.Name);

                // Update CityId and VenueId from db
                City.Id = await _cityRepository.GetCityIdAsync(City.Name);
                Venue.Id = await _venueRepository.GetVenueIdAsync(Venue.Name, City.Id);

                // Iterate trough each event 
                int eventIndex = 0;
                foreach (var eventEl in eventElements)
                {
                    eventIndex++;
                    _logger.LogInformation("Parsing event {index}/{total}...", eventIndex, eventElements.Count);

                    try
                    {
                        var newEvent = new Event
                        {
                            EventCity = City,
                            EventVenue = Venue
                        };

                        // Get basic details from the event
                        var titleEl = await eventEl.QuerySelectorAsync(".cl-event-card__title a");
                        var detailUrl = await titleEl?.GetAttributeAsync("href") ?? "";

                        if (string.IsNullOrEmpty(detailUrl))
                        {
                            _logger.LogWarning("No detailpage URL found for the event.");
                            continue;
                        }

                        if (!IsValidUrl(detailUrl))
                        {
                            _logger.LogWarning("Blocked potentially malicious URL: {url}", detailUrl);
                            continue;
                        }

                        // Parse event details
                        newEvent = await ParseEventDetailPage(detailUrl, newEvent, browser);

                        if (!Events.Exists(e => e.Equals(newEvent)))
                        {
                            Events.Add(newEvent);
                        }

                        if (eventIndex < eventElements.Count)
                        {
                            var delayMs = Delay.CalculateSeconds();
                            _logger.LogInformation("Waiting {delay}ms before next request ({current}/{total})...",
                                delayMs, eventIndex + 1, eventElements.Count);

                            await Task.Delay(delayMs);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "An error occurred while parsing event page.");
                    }
                }

                _logger.LogInformation("Parsed {events.count} events from {venue}.", Events.Count, Venue.Name);
                return Events;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while scraping {venue} events.", Venue.Name);
                return Events;
            }
        }

        private async Task<Event> ParseEventDetailPage(string url, Event newEvent, IBrowser browser)
        {
            try
            {
                _logger.LogInformation("Opening detail page: {url}", url);

                var detailPage = await browser.NewPageAsync();
                await detailPage.GotoAsync(url, new()
                {
                    Timeout = 60000,
                    WaitUntil = WaitUntilState.DOMContentLoaded
                });

                var detailsEl = await detailPage?.QuerySelectorAsync(".product-content-text");
                var h1El = await detailPage.QuerySelectorAsync("h1");
                var details = await detailsEl.InnerTextAsync();
                var h1Text = await h1El?.InnerTextAsync() ?? "";
                var h1Elements = h1Text.Split(' ');
                var index = 0;

                // Parse artist
                var artistString = string.Empty;
                for (int i = index; i < h1Elements.Length; i++)
                {
                    if (DateHelper.IsWeekdayPrefix(h1Elements[i]))
                    {
                        index++;
                        break;
                    }

                    if (artistString.Length != 0)
                    {
                        artistString += " ";
                    }

                    artistString += h1Elements[i];
                    index++;
                }

                // Parse showtime & price
                var showtime = ParseShowtime(h1Elements[index], h1Elements[index + 2]);
                var price = ParsePrice(details);

                // Extract parsed data for the event object
                newEvent.Artist = _cleaner.Clean(artistString);
                newEvent.Showtime = showtime;
                newEvent.HasShowtime = true;
                newEvent.Price = price;

                await detailPage.CloseAsync();
                return newEvent;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing detail page: {url}", url);
                return newEvent;
            }
        }

        private string ParsePrice(string priceString)
        {
            var price = string.Empty;
            var strings = priceString.Split("\n");

            foreach (var item in strings)
            {
                if (!item.Contains("Liput"))
                {
                    continue;
                }

                strings = item.Split(' ');
                break;
            }

            var prices = strings[1].Split('/');

            foreach (var item in prices)
            {
                if (price.Length != 0)
                {
                    price += " / ";
                }

                price += item + "€";
            }

            return price;
        }

        private DateTime ParseShowtime(string date, string time)
        {
            var now = DateTime.Now;
            int year, month, day, hours, minutes;

            var dates = date.Split('.');
            day = int.Parse(dates[0].Trim());
            month = int.Parse(dates[1].Trim());
            year = int.Parse(dates[2].Trim());

            if (time.Contains(':'))
            {
                hours = int.Parse(time.Split(':')[0].Trim());
                minutes = int.Parse(time.Split(':')[1].Trim());
            }
            else
            {
                hours = int.Parse(time.Trim());
                minutes = 0;
            }

            // Adjust year if the date has already passed this year
            year = DateHelper.AdjustYear(month, day);

            return new DateTime(year, month, day, hours, minutes, 0);
        }
    }
}
