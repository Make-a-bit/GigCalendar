using HtmlAgilityPack;
using Scraper.Models;

namespace Scraper.Services.Scrapers
{
    public class BaseScraper
    {
        public Venue Venue { get; set; }
        public City City { get; set; }
        public List<Event> Events { get; set; }
        public HtmlDocument Doc { get; set; }

        public BaseScraper()
        {
            Venue = new Venue();
            City = new City();
            Events = new List<Event>();
            Doc = new HtmlDocument();
        }
    }
}
