using System.Globalization;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using Scraper.Models;
using Scraper.Repositories;

namespace Scraper.Services.Scrapers
{
    public class KoteloScraper : BaseScraper, IEventScraper
    {
        private readonly ICleaner _cleaner;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger _logger;
        private readonly ICityRepository _cityRepository;
        private readonly IVenueRepository _venueRepository;

        public KoteloScraper(ICleaner cleaner,
            IHttpClientFactory httpClientFactory,
            ICityRepository cityRepository,
            IVenueRepository venueRepository,
            ILogger<PatoklubiScraper> logger)
        {
            _cleaner = cleaner;
            _httpClientFactory = httpClientFactory;
            _cityRepository = cityRepository;
            _venueRepository = venueRepository;
            _logger = logger;

            City.Name = "Tampere";
            Venue.Name = "Bar Kotelo";
        }

        public async Task<List<Event>> ScrapeEvents()
        {
            try
            {
                _logger.LogInformation("Starting to scrape {venue} events...", Venue.Name);

                using var client = _httpClientFactory.CreateClient();
                var url = "https://barkotelo.fi/Keikat/#keikkasivu";
                var html = await client.GetStringAsync(url);
                Doc.LoadHtml(html);

                var nodes = Doc.DocumentNode
                    .SelectNodes("//div[contains(@class,'wb-layout-horizontal')][.//div[contains(@class,'wb_text_element')]]")
                    ?.ToList() ?? new List<HtmlNode>();

                // Etsii päivämäärän muodossa dd.mm.yyyy (käytetään eventtien tunnistamiseen)
                nodes = nodes
                    .Where(n => Regex.IsMatch(n.InnerText, @"\d{1,2}\.\d{1,2}\.\d{4}"))
                    .ToList();

                _logger.LogInformation("Found {events.count} nodes from Kotelo", nodes.Count);
                _logger.LogInformation("Starting to parse event details...");

                City.Id = await _cityRepository.GetCityIdAsync(City.Name);
                Venue.Id = await _venueRepository.GetVenueIdAsync(Venue.Name, City.Id);

                const string DatePattern = @"\d{1,2}\.\d{1,2}\.\d{4}";
                var culture = CultureInfo.InvariantCulture;
                var showtime = new DateTime();
                var price = string.Empty;
                var artist = string.Empty;

                foreach (var n in nodes)
                {
                    var textNode = n.SelectSingleNode(".//div[contains(@class, 'wb_text_element')]");
                    if (textNode == null) continue;

                    var h4s = textNode.SelectNodes(".//h4");
                    if (h4s == null) continue;

                    var rows = h4s
                        .Select(h => HtmlEntity.DeEntitize(h.InnerText).Trim())
                        .Where(r => !string.IsNullOrWhiteSpace(r))
                        .ToList();

                    var date = rows.FirstOrDefault(r => Regex.IsMatch(r, DatePattern));
                    var priceRow = rows.FirstOrDefault(r => r.ToLower().Contains("liput"));
                    var timeRow = rows.FirstOrDefault(r => r.ToLower().Contains("klo"));

                    var artists = rows
                        .Where(r =>
                            r != date &&
                            r != priceRow &&
                            r != timeRow)
                        .ToList();

                    if (date != null && timeRow != null)
                    {
                        showtime = ParseShowtime(date, timeRow);
                    }

                    var newEvent = new Event
                    {
                        EventCity = City,
                        EventVenue = Venue,
                        Artist = string.Join(", ", artists.Select(FormatArtist)),
                        Showtime = showtime,
                        HasShowtime = true,
                        Price = priceRow != null ? ParseEventprice(priceRow) : string.Empty,
                    };

                    _logger.LogInformation("Parsed event: {newEvent}", newEvent.ToString());

                    // Compare if events already contains the new scraped event.
                    // If not, add it to the list.
                    if (!Events.Exists(e => e.Equals(newEvent)))
                    {
                        Events.Add(newEvent);
                    }
                }

                _logger.LogInformation("Parsed {events.Count} events from {venue.Name}", Events.Count, Venue.Name);

                return Events;
            }
            catch (Exception ex) 
            {
                _logger.LogError(ex, "Error occurred while scraping events from {venue}", Venue.Name);
                return Events;
            }
        }

        private static string FormatArtist(string input)
        {
            var culture = CultureInfo.InvariantCulture;
            var trimmed = input.Trim();
            if (string.IsNullOrWhiteSpace(trimmed))
                return trimmed;

            var parts = trimmed.Split('(');

            var namePart = parts[0].Trim();
            var titleCased = culture.TextInfo.ToTitleCase(namePart.ToLower());

            return parts.Length > 1
                ? $"{titleCased} ({parts[1]}"
                : titleCased;
        }

        private static string ParseEventprice(string details)
        {
            var detailStrings = details.Split(":");
            var priceStrings = detailStrings[1].Split("/");
            var price = string.Empty;

            foreach (var p in priceStrings)
            {
                if (!p.Contains('€'))
                {
                    price += p + "€" + " / ";
                    continue;
                }
                price += p;
            }
            return price;
        }

        private static DateTime ParseShowtime(string date, string time)
        {
            var parsedDate = DateTime.ParseExact(
                date.Trim(),
                "d.M.yyyy",
                CultureInfo.InvariantCulture
            );

            var match = Regex.Match(time, @"klo\s*(\d{1,2})", RegexOptions.IgnoreCase);

            if (!match.Success)
                throw new Exception($"Invalid time format: {time}");

            var hour = int.Parse(match.Groups[1].Value);

            return new DateTime(
                parsedDate.Year,
                parsedDate.Month,
                parsedDate.Day,
                hour,
                0,
                0
            );
        }
    }
}