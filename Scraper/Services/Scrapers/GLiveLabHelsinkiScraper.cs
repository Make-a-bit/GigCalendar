using Microsoft.Extensions.Logging;
using Scraper.Models;
using Scraper.Repositories;

namespace Scraper.Services.Scrapers
{
    public class GLiveLabHelsinkiScraper : GLiveLabBaseScraper
    {
        protected override string ScrapeUrl => "https://glivelab.fi/?show_all=1";

        public GLiveLabHelsinkiScraper(ICleaner cleaner,
            IHttpClientFactory httpClient,
            ICityRepository cityRepository,
            IVenueRepository venueRepository,
            ILogger<GLiveLabHelsinkiScraper> logger)
            : base(cleaner, httpClient, cityRepository, venueRepository, logger)
        {
            Venue.Name = "G Livelab Helsinki";
            City.Name = "Helsinki";
        }
    }
}