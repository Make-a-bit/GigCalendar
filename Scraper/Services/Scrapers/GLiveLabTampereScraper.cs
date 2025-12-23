using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using Scraper.Models;
using Scraper.Repositories;
using System.Threading.Tasks;

namespace Scraper.Services.Scrapers
{
    public class GLiveLabTampereScraper : GLiveLabBaseScraper
    {
        protected override string ScrapeUrl => "https://glivelab.fi/tampere/?show_all=1";

        public GLiveLabTampereScraper(ICleaner cleaner,
            IHttpClientFactory httpClientFactory,
            ICityRepository cityRepository,
            IVenueRepository venueRepository,
            ILogger<GLiveLabTampereScraper> logger)
            : base(cleaner, httpClientFactory, cityRepository, venueRepository, logger)
        {
            Venue.Name = "G Livelab Tampere";
            City.Name = "Tampere";
        }
    }
}