using Microsoft.Extensions.Logging;
using Scraper.Models;
using Scraper.Repositories;
using System.Net.Http;

namespace Scraper.Services.Scrapers
{
    public class OnTheRocksScraper : MediaaniScraper
    {
        protected override string ScrapeUrl => "https://www.rocks.fi/tapahtumat/";

        public OnTheRocksScraper(ICleaner cleaner,
            IHttpClientFactory httpClientFactory,
            ILogger<OnTheRocksScraper> logger,
            ICityRepository cityRepository,
            IVenueRepository venueRepository)
            : base(cleaner, httpClientFactory, cityRepository, venueRepository, logger)
        {
            City.Name = "Helsinki";
            Venue.Name = "On The Rocks";
        }
    }
}
