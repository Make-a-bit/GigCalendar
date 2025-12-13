namespace Scraper.Services
{
    public interface ICleaner
    {
        string EventCleaner(string input);
        string PriceCleaner(string input);
    }
}
