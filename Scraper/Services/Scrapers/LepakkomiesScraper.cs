using Microsoft.Extensions.Logging;
using Scraper.Models;
using Scraper.Repositories;

namespace Scraper.Services.Scrapers
{
    public class LepakkomiesScraper : MediaaniScraper
    {
        protected override string ScrapeUrl => "https://www.lepis.fi/tapahtumat/";

        public LepakkomiesScraper(ICleaner cleaner,
            IHttpClientFactory httpClientFactory,
            ILogger<OnTheRocksScraper> logger,
            ICityRepository cityRepository,
            IVenueRepository venueRepository)
            : base(cleaner, httpClientFactory, cityRepository, venueRepository, logger)
        {
            City.Name = "Helsinki";
            Venue.Name = "Lepakkomies";
        }
    }
}
